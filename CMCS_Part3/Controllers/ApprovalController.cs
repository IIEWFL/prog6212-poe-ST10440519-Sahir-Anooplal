using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMCS_Part3.Models;
using CMCS_Part3.Services;

namespace CMCS_Part3.Controllers
{
    public class ApprovalController : Controller
    {
        private readonly CMCSDbContext _context;
        private readonly IApprovalService _approvalService;
        private readonly IUserService _userService;

        public ApprovalController(CMCSDbContext context, IApprovalService approvalService, IUserService userService)
        {
            _context = context;
            _approvalService = approvalService;
            _userService = userService;
        }

        // GET: Approval - Shows all pending claims with automated validation
        public async Task<IActionResult> Index()
        {
            if (!_userService.CanAccessApproval())
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var currentUser = _userService.GetCurrentUser();
            var pendingClaims = await _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.SupportingDocuments)
                .Where(c => c.Status == ClaimStatus.Pending)
                .OrderBy(c => c.SubmissionDate)
                .ToListAsync();

            // Run automated validation on all claims
            var claimsWithValidation = new List<ClaimWithValidation>();
            foreach (var claim in pendingClaims)
            {
                var validationResult = await _approvalService.ValidateClaimAsync(claim);
                claimsWithValidation.Add(new ClaimWithValidation
                {
                    Claim = claim,
                    ValidationResult = validationResult
                });
            }

            ViewData["CurrentUser"] = currentUser;
            ViewData["ApprovalCriteria"] = _approvalService.GetApprovalCriteria();

            return View(claimsWithValidation);
        }

        // GET: Approval/All - Shows all claims with status
        public async Task<IActionResult> All()
        {
            if (!_userService.CanAccessApproval())
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var allClaims = await _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.SupportingDocuments)
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();

            ViewData["CurrentUser"] = _userService.GetCurrentUser();
            return View(allClaims);
        }

        // GET: Approval/Details/5 - Claim details with automated validation
        public async Task<IActionResult> Details(int? id)
        {
            if (!_userService.CanAccessApproval())
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var claim = await _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.SupportingDocuments)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (claim == null)
            {
                return NotFound();
            }

            var validationResult = await _approvalService.ValidateClaimAsync(claim);
            var autoApproveClaims = await _approvalService.GetClaimsForAutoApprovalAsync();
            var canAutoApprove = autoApproveClaims.Any(c => c.Id == id);

            ViewData["CurrentUser"] = _userService.GetCurrentUser();
            ViewData["ValidationResult"] = validationResult;
            ViewData["CanAutoApprove"] = canAutoApprove;
            ViewData["ApprovalCriteria"] = _approvalService.GetApprovalCriteria();

            return View(claim);
        }

        // POST: Approval/Approve/5 - With automated validation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, bool? autoApprove = false)
        {
            if (!_userService.CanAccessApproval())
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var currentUser = _userService.GetCurrentUser();
            if (currentUser == null)
            {
                return RedirectToAction("SelectRole", "Home");
            }

            try
            {
                ApprovalWorkflowResult result;

                if (autoApprove == true)
                {
                    var claim = await _context.Claims.FindAsync(id);
                    if (claim != null)
                    {
                        var autoApproved = await _approvalService.AutoApproveClaimAsync(claim, currentUser.Name);
                        if (autoApproved)
                        {
                            TempData["SuccessMessage"] = $"Claim #{id} was auto-approved successfully!";
                        }
                        else
                        {
                            TempData["ErrorMessage"] = $"Claim #{id} does not meet auto-approval criteria.";
                        }
                    }
                }
                else
                {
                    result = await _approvalService.ProcessApprovalWorkflowAsync(id, currentUser.Name, true);

                    if (result.Success)
                    {
                        TempData["SuccessMessage"] = result.Message;
                    }
                    else
                    {
                        TempData["ErrorMessage"] = result.Message;
                    }
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while approving the claim.";
                // Log the exception in a real application
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Approval/Reject/5 - With reason
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string rejectionReason)
        {
            if (!_userService.CanAccessApproval())
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var currentUser = _userService.GetCurrentUser();
            if (currentUser == null)
            {
                return RedirectToAction("SelectRole", "Home");
            }

            try
            {
                var result = await _approvalService.ProcessApprovalWorkflowAsync(id, currentUser.Name, false, rejectionReason);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while rejecting the claim.";
                // Log the exception in a real application
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Approval/BulkAutoApprove - Auto-approve all eligible claims
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkAutoApprove()
        {
            if (!_userService.CanAccessApproval())
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var currentUser = _userService.GetCurrentUser();
            var autoApproveClaims = await _approvalService.GetClaimsForAutoApprovalAsync();
            var approvedCount = 0;

            foreach (var claim in autoApproveClaims)
            {
                var success = await _approvalService.AutoApproveClaimAsync(claim, currentUser?.Name ?? "System");
                if (success) approvedCount++;
            }

            if (approvedCount > 0)
            {
                TempData["SuccessMessage"] = $"Auto-approved {approvedCount} claims successfully!";
            }
            else
            {
                TempData["InfoMessage"] = "No claims met the auto-approval criteria.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Approval/AutoApproveList - Show claims eligible for auto-approval
        public async Task<IActionResult> AutoApproveList()
        {
            if (!_userService.CanAccessApproval())
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var autoApproveClaims = await _approvalService.GetClaimsForAutoApprovalAsync();
            ViewData["CurrentUser"] = _userService.GetCurrentUser();

            return View(autoApproveClaims);
        }
    }

    // Helper class for views
    public class ClaimWithValidation
    {
        public Claim Claim { get; set; } = null!;
        public ApprovalValidationResult ValidationResult { get; set; } = null!;
    }
}