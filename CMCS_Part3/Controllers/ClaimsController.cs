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

        public ClaimsController(CMCSDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        // GET: Claims - Shows list of claims for the CURRENT lecturer
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

            ViewData["CurrentUser"] = currentUser;
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

            return View(claim);
        }

        // POST: Claims/Create - Handle claim submission with enhanced validation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Claim claim, IFormFile? supportingDocument)
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

                    // Handle file upload if present
                    if (supportingDocument != null && supportingDocument.Length > 0)
                    {
                        await HandleFileUpload(supportingDocument, claim.Id);
                    }

                    TempData["SuccessMessage"] = $"Claim submitted successfully! Total amount: R{claim.TotalAmount:N2}";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    TempData["ErrorMessage"] = "An error occurred while submitting your claim. Please try again.";
                }
            }

            // If something failed then redisplay form
            return View(claim);
        }

        // GET: Claims/Details/5 - View claim details
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

            ViewData["CurrentUser"] = currentUser;
            return View(claim);
        }

        private async Task HandleFileUpload(IFormFile file, int claimId)
        {
            // Validate file type and size
            var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                TempData["ErrorMessage"] = "Invalid file type. Only PDF, DOCX, and XLSX files are allowed.";
                return;
            }

            if (file.Length > 5 * 1024 * 1024) // 5MB limit
            {
                TempData["ErrorMessage"] = "File size too large. Maximum size is 5MB.";
                return;
            }

            // Generate unique filename
            var storedFileName = $"{Guid.NewGuid()}{fileExtension}";
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var filePath = Path.Combine(uploadsFolder, storedFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Save document info to database
            var document = new SupportingDocument
            {
                FileName = file.FileName,
                StoredFileName = storedFileName,
                FileSize = file.Length,
                FileType = fileExtension,
                ClaimId = claimId
            };

            _context.SupportingDocuments.Add(document);
            await _context.SaveChangesAsync();
        }

        private string GetMonthName(string monthString)
        {
            if (DateTime.TryParseExact(monthString, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out var date))
            {
                return date.ToString("MMMM yyyy");
            }
            return monthString;
        }
    }
}