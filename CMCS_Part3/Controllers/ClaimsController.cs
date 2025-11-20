using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMCS_Part3.Models;
using CMCS_Part3.Services;

namespace CMCS_Part3.Controllers
{
    public class ClaimsController : Controller
    {
        private readonly CMCSDbContext _context;
        private readonly IUserService _userService;
        private readonly IFileService _fileService;
        private readonly IStatusService _statusService;

        public ClaimsController(CMCSDbContext context, IUserService userService, IFileService fileService, IStatusService statusService)
        {
            _context = context;
            _userService = userService;
            _fileService = fileService;
            _statusService = statusService;
        }

        // GET: Claims - Shows list of claims for the CURRENT lecturer with enhanced status tracking
        public async Task<IActionResult> Index()
        {
            if (!_userService.CanAccessClaims())
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var currentUser = _userService.GetCurrentUser();
            if (currentUser == null)
            {
                return RedirectToAction("SelectRole", "Home");
            }

            var claims = await _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.SupportingDocuments)
                .Where(c => c.LecturerId == currentUser.LecturerId)
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();

            // Get status overview for the current user
            var statusOverview = await _statusService.GetStatusOverviewAsync();
            var overdueClaims = await _statusService.GetOverdueClaimsAsync();
            var userOverdueClaims = overdueClaims.Where(c => c.LecturerId == currentUser.LecturerId).ToList();

            ViewData["CurrentUser"] = currentUser;
            ViewData["SupportedFileTypes"] = _fileService.GetSupportedFileTypes();
            ViewData["MaxFileSize"] = _fileService.GetMaxFileSize();
            ViewData["StatusOverview"] = statusOverview;
            ViewData["OverdueClaims"] = userOverdueClaims;
            ViewData["TotalOverdue"] = userOverdueClaims.Count;

            return View(claims);
        }

        // GET: Claims/Create - Show claim submission form with automation
        public IActionResult Create()
        {
            if (!_userService.CanAccessClaims())
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var currentUser = _userService.GetCurrentUser();
            if (currentUser == null)
            {
                return RedirectToAction("SelectRole", "Home");
            }

            // Pre-populate with current month
            var claim = new Claim
            {
                ClaimMonth = DateTime.Now.ToString("yyyy-MM"),
                HoursWorked = 0,
                HourlyRate = 0
            };

            ViewData["SupportedFileTypes"] = _fileService.GetSupportedFileTypes();
            ViewData["MaxFileSize"] = _fileService.GetMaxFileSize();

            return View(claim);
        }

        // POST: Claims/Create - Handle claim submission with enhanced validation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Claim claim, List<IFormFile> supportingDocuments)
        {
            if (!_userService.CanAccessClaims())
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var currentUser = _userService.GetCurrentUser();
            if (currentUser == null)
            {
                return RedirectToAction("SelectRole", "Home");
            }

            // Automated validation checks
            if (claim.HoursWorked > 200)
            {
                ModelState.AddModelError("HoursWorked", "Hours worked cannot exceed 200 hours per month.");
            }

            if (claim.HourlyRate < 100 || claim.HourlyRate > 1000)
            {
                ModelState.AddModelError("HourlyRate", "Hourly rate must be between R100 and R1000.");
            }

            // Check for duplicate claims for same month
            var existingClaim = await _context.Claims
                .FirstOrDefaultAsync(c => c.LecturerId == currentUser.LecturerId &&
                                         c.ClaimMonth == claim.ClaimMonth);

            if (existingClaim != null)
            {
                ModelState.AddModelError("ClaimMonth", $"You have already submitted a claim for {GetMonthName(claim.ClaimMonth)}.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Set automated properties
                    claim.LecturerId = currentUser.LecturerId;
                    claim.Status = ClaimStatus.Pending;
                    claim.SubmissionDate = DateTime.Now;

                    _context.Add(claim);
                    await _context.SaveChangesAsync();

                    // Handle file uploads if present
                    if (supportingDocuments != null && supportingDocuments.Any(f => f.Length > 0))
                    {
                        foreach (var file in supportingDocuments.Where(f => f.Length > 0))
                        {
                            var uploadResult = await _fileService.UploadFileAsync(file, claim.Id);
                            if (!uploadResult.Success)
                            {
                                TempData["WarningMessage"] = $"Some files could not be uploaded: {uploadResult.Message}";
                            }
                        }
                    }

                    // Send status notification
                    await _statusService.SendStatusNotificationAsync(claim, "New claim submitted for review");

                    TempData["SuccessMessage"] = $"Claim submitted successfully! Total amount: R{claim.TotalAmount:N2}";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    TempData["ErrorMessage"] = "An error occurred while submitting your claim. Please try again.";
                }
            }

            ViewData["SupportedFileTypes"] = _fileService.GetSupportedFileTypes();
            ViewData["MaxFileSize"] = _fileService.GetMaxFileSize();

            return View(claim);
        }

        // GET: Claims/Details/5 - View claim details with status history
        public async Task<IActionResult> Details(int? id)
        {
            if (!_userService.CanAccessClaims())
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var currentUser = _userService.GetCurrentUser();
            var claim = await _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.SupportingDocuments)
                .FirstOrDefaultAsync(c => c.Id == id && c.LecturerId == currentUser!.LecturerId);

            if (claim == null)
            {
                return NotFound();
            }

            // Get status history
            var statusHistory = await _statusService.GetStatusHistoryAsync(claim.Id);
            var isOverdue = await _statusService.GetOverdueClaimsAsync();
            var claimIsOverdue = isOverdue.Any(c => c.Id == claim.Id);

            ViewData["CurrentUser"] = currentUser;
            ViewData["FileService"] = _fileService;
            ViewData["StatusHistory"] = statusHistory;
            ViewData["IsOverdue"] = claimIsOverdue;
            ViewData["DaysPending"] = (DateTime.Now - claim.SubmissionDate).Days;

            return View(claim);
        }

        // GET: Claims/Status/5 - Status tracking page
        public async Task<IActionResult> Status(int? id)
        {
            if (!_userService.CanAccessClaims())
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var currentUser = _userService.GetCurrentUser();
            var claim = await _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.SupportingDocuments)
                .FirstOrDefaultAsync(c => c.Id == id && c.LecturerId == currentUser!.LecturerId);

            if (claim == null)
            {
                return NotFound();
            }

            var statusHistory = await _statusService.GetStatusHistoryAsync(claim.Id);
            var statusOverview = await _statusService.GetStatusOverviewAsync();

            ViewData["CurrentUser"] = currentUser;
            ViewData["StatusHistory"] = statusHistory;
            ViewData["StatusOverview"] = statusOverview;
            ViewData["EstimatedApprovalTime"] = GetEstimatedApprovalTime(claim);

            return View(claim);
        }

        // POST: Claims/DeleteDocument/5 - Delete a document
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            if (!_userService.CanAccessClaims())
            {
                return Json(new { success = false, message = "Access denied" });
            }

            var currentUser = _userService.GetCurrentUser();
            var document = await _context.SupportingDocuments
                .Include(d => d.Claim)
                .FirstOrDefaultAsync(d => d.Id == id && d.Claim!.LecturerId == currentUser!.LecturerId);

            if (document == null)
            {
                return Json(new { success = false, message = "Document not found" });
            }

            var result = await _fileService.DeleteFileAsync(document.StoredFileName);

            if (result)
            {
                return Json(new { success = true, message = "Document deleted successfully" });
            }
            else
            {
                return Json(new { success = false, message = "Failed to delete document" });
            }
        }

        // GET: Claims/Overdue - Show overdue claims
        public async Task<IActionResult> Overdue()
        {
            if (!_userService.CanAccessClaims())
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var currentUser = _userService.GetCurrentUser();
            var overdueClaims = await _statusService.GetOverdueClaimsAsync();
            var userOverdueClaims = overdueClaims.Where(c => c.LecturerId == currentUser!.LecturerId).ToList();

            ViewData["CurrentUser"] = currentUser;
            ViewData["TotalOverdue"] = userOverdueClaims.Count;

            return View(userOverdueClaims);
        }

        private string GetMonthName(string monthString)
        {
            if (DateTime.TryParseExact(monthString, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out var date))
            {
                return date.ToString("MMMM yyyy");
            }
            return monthString;
        }

        private string GetEstimatedApprovalTime(Claim claim)
        {
            var daysPending = (DateTime.Now - claim.SubmissionDate).Days;

            if (claim.Status == ClaimStatus.Approved)
                return "Approved";

            if (claim.Status == ClaimStatus.Rejected)
                return "Rejected";

            if (daysPending < 7)
                return "1-2 weeks";
            else if (daysPending < 14)
                return "This week";
            else if (daysPending < 30)
                return "Overdue - Please follow up";
            else
                return "Significantly overdue - Contact administrator";
        }
    }
}