using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMCS_Part3.Models;
using CMCS_Part3.Services;

namespace CMCS_Part3.Controllers
{
    public class HRController : Controller
    {
        private readonly CMCSDbContext _context;
        private readonly IUserService _userService;

        public HRController(CMCSDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        // GET: HR - HR Dashboard
        public IActionResult Index()
        {
            if (!_userService.CanAccessHR())
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var currentUser = _userService.GetCurrentUser();
            ViewData["CurrentUser"] = currentUser;

            // Get dashboard statistics
            var stats = new HRDashboardStats
            {
                TotalLecturers = _context.Lecturers.Count(),
                ActiveLecturers = _context.Lecturers.Count(l => l.IsActive),
                TotalClaims = _context.Claims.Count(),
                PendingClaims = _context.Claims.Count(c => c.Status == ClaimStatus.Pending),
                ApprovedClaims = _context.Claims.Count(c => c.Status == ClaimStatus.Approved),
                TotalAmountApproved = _context.Claims
                    .Where(c => c.Status == ClaimStatus.Approved)
                    .Sum(c => c.TotalAmount)
            };

            return View(stats);
        }

        // GET: HR/Lecturers - Manage lecturers
        public async Task<IActionResult> Lecturers()
        {
            if (!_userService.CanAccessHR())
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var lecturers = await _context.Lecturers
                .OrderBy(l => l.Name)
                .ToListAsync();

            ViewData["CurrentUser"] = _userService.GetCurrentUser();
            return View(lecturers);
        }

        // GET: HR/CreateLecturer - Create new lecturer form
        public IActionResult CreateLecturer()
        {
            if (!_userService.CanAccessHR())
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            return View();
        }

        // POST: HR/CreateLecturer - Create new lecturer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLecturer(Lecturer lecturer)
        {
            if (!_userService.CanAccessHR())
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check for duplicate email
                    var existingLecturer = await _context.Lecturers
                        .FirstOrDefaultAsync(l => l.Email == lecturer.Email);

                    if (existingLecturer != null)
                    {
                        ModelState.AddModelError("Email", "A lecturer with this email already exists.");
                        return View(lecturer);
                    }

                    lecturer.CreatedDate = DateTime.Now;
                    lecturer.IsActive = true;

                    _context.Add(lecturer);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Lecturer {lecturer.Name} created successfully!";
                    return RedirectToAction(nameof(Lecturers));
                }
                catch (Exception)
                {
                    TempData["ErrorMessage"] = "An error occurred while creating the lecturer.";
                }
            }

            return View(lecturer);
        }

        // GET: HR/EditLecturer/5 - Edit lecturer
        public async Task<IActionResult> EditLecturer(int? id)
        {
            if (!_userService.CanAccessHR())
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var lecturer = await _context.Lecturers.FindAsync(id);
            if (lecturer == null)
            {
                return NotFound();
            }

            return View(lecturer);
        }

        // POST: HR/EditLecturer/5 - Update lecturer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLecturer(int id, Lecturer lecturer)
        {
            if (!_userService.CanAccessHR())
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            if (id != lecturer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check for duplicate email (excluding current lecturer)
                    var existingLecturer = await _context.Lecturers
                        .FirstOrDefaultAsync(l => l.Email == lecturer.Email && l.Id != id);

                    if (existingLecturer != null)
                    {
                        ModelState.AddModelError("Email", "A lecturer with this email already exists.");
                        return View(lecturer);
                    }

                    _context.Update(lecturer);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Lecturer {lecturer.Name} updated successfully!";
                    return RedirectToAction(nameof(Lecturers));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LecturerExists(lecturer.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception)
                {
                    TempData["ErrorMessage"] = "An error occurred while updating the lecturer.";
                }
            }

            return View(lecturer);
        }

        // POST: HR/ToggleLecturerStatus/5 - Toggle active status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLecturerStatus(int id)
        {
            if (!_userService.CanAccessHR())
            {
                return Json(new { success = false, message = "Access denied" });
            }

            try
            {
                var lecturer = await _context.Lecturers.FindAsync(id);
                if (lecturer == null)
                {
                    return Json(new { success = false, message = "Lecturer not found" });
                }

                lecturer.IsActive = !lecturer.IsActive;
                await _context.SaveChangesAsync();

                var action = lecturer.IsActive ? "activated" : "deactivated";
                return Json(new { success = true, message = $"Lecturer {action} successfully", isActive = lecturer.IsActive });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while updating the lecturer status" });
            }
        }

        // GET: HR/Reports - Reports dashboard
        public IActionResult Reports()
        {
            if (!_userService.CanAccessHR())
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            ViewData["CurrentUser"] = _userService.GetCurrentUser();

            // Get recent reports
            var reports = _context.Reports
                .OrderByDescending(r => r.GeneratedDate)
                .Take(5)
                .ToList();

            return View(reports);
        }

        // GET: HR/GenerateReport - Report generation form
        public IActionResult GenerateReport()
        {
            if (!_userService.CanAccessHR())
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            // Pre-populate with default date range (current month)
            var report = new Report
            {
                DateFrom = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                DateTo = DateTime.Now
            };

            return View(report);
        }

        // POST: HR/GenerateReport - Generate report
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateReport(Report report)
        {
            if (!_userService.CanAccessHR())
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Generate report data based on type
                    var reportData = await GenerateReportData(report);

                    // Create report record
                    report.GeneratedDate = DateTime.Now;
                    report.TotalAmount = reportData.TotalAmount;
                    report.TotalClaims = reportData.TotalClaims;

                    _context.Reports.Add(report);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Report '{report.ReportName}' generated successfully!";
                    return RedirectToAction(nameof(ReportDetails), new { id = report.Id });
                }
                catch (Exception)
                {
                    TempData["ErrorMessage"] = "An error occurred while generating the report.";
                }
            }

            return View(report);
        }

        // GET: HR/ReportDetails/5 - View report details
        public async Task<IActionResult> ReportDetails(int? id)
        {
            if (!_userService.CanAccessHR())
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var report = await _context.Reports.FindAsync(id);
            if (report == null)
            {
                return NotFound();
            }

            // Get report data
            var reportData = await GenerateReportData(report);

            ViewData["CurrentUser"] = _userService.GetCurrentUser();
            ViewData["ReportData"] = reportData;

            return View(report);
        }

        // GET: HR/ClaimAnalysis - Claim analysis dashboard
        public async Task<IActionResult> ClaimAnalysis()
        {
            if (!_userService.CanAccessHR())
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var analysis = new ClaimAnalysis
            {
                // Monthly trends
                MonthlyTrends = await GetMonthlyTrends(),

                // Department breakdown
                DepartmentBreakdown = await GetDepartmentBreakdown(),

                // Lecturer performance
                TopPerformers = await GetTopPerformers(),

                // Status distribution
                StatusDistribution = await GetStatusDistribution()
            };

            ViewData["CurrentUser"] = _userService.GetCurrentUser();
            return View(analysis);
        }

        private bool LecturerExists(int id)
        {
            return _context.Lecturers.Any(e => e.Id == id);
        }

        private async Task<ReportData> GenerateReportData(Report report)
        {
            var claims = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.SubmissionDate >= report.DateFrom && c.SubmissionDate <= report.DateTo)
                .ToListAsync();

            var reportData = new ReportData
            {
                TotalClaims = claims.Count,
                TotalAmount = claims.Sum(c => c.TotalAmount),
                ApprovedClaims = claims.Count(c => c.Status == ClaimStatus.Approved),
                PendingClaims = claims.Count(c => c.Status == ClaimStatus.Pending),
                RejectedClaims = claims.Count(c => c.Status == ClaimStatus.Rejected),
                Claims = claims
            };

            return reportData;
        }

        private async Task<List<MonthlyTrend>> GetMonthlyTrends()
        {
            var trends = await _context.Claims
                .Where(c => c.SubmissionDate >= DateTime.Now.AddMonths(-6))
                .GroupBy(c => new { c.SubmissionDate.Year, c.SubmissionDate.Month })
                .Select(g => new MonthlyTrend
                {
                    Month = new DateTime(g.Key.Year, g.Key.Month, 1),
                    TotalClaims = g.Count(),
                    TotalAmount = g.Sum(c => c.TotalAmount),
                    AverageAmount = g.Average(c => c.TotalAmount)
                })
                .OrderBy(t => t.Month)
                .ToListAsync();

            return trends;
        }

        private async Task<List<DepartmentBreakdown>> GetDepartmentBreakdown()
        {
            var breakdown = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Lecturer != null && !string.IsNullOrEmpty(c.Lecturer.Department))
                .GroupBy(c => c.Lecturer!.Department)
                .Select(g => new DepartmentBreakdown
                {
                    Department = g.Key!,
                    TotalClaims = g.Count(),
                    TotalAmount = g.Sum(c => c.TotalAmount),
                    AverageAmount = g.Average(c => c.TotalAmount)
                })
                .OrderByDescending(b => b.TotalAmount)
                .ToListAsync();

            return breakdown;
        }

        private async Task<List<LecturerPerformance>> GetTopPerformers()
        {
            var performers = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Lecturer != null && c.Status == ClaimStatus.Approved)
                .GroupBy(c => new { c.LecturerId, c.Lecturer!.Name })
                .Select(g => new LecturerPerformance
                {
                    LecturerName = g.Key.Name,
                    TotalClaims = g.Count(),
                    TotalAmount = g.Sum(c => c.TotalAmount),
                    AverageAmount = g.Average(c => c.TotalAmount)
                })
                .OrderByDescending(p => p.TotalAmount)
                .Take(10)
                .ToListAsync();

            return performers;
        }

        private async Task<StatusDistribution> GetStatusDistribution()
        {
            var distribution = new StatusDistribution
            {
                Pending = await _context.Claims.CountAsync(c => c.Status == ClaimStatus.Pending),
                Approved = await _context.Claims.CountAsync(c => c.Status == ClaimStatus.Approved),
                Rejected = await _context.Claims.CountAsync(c => c.Status == ClaimStatus.Rejected)
            };

            distribution.Total = distribution.Pending + distribution.Approved + distribution.Rejected;

            return distribution;
        }
    }

    // Helper classes for HR module
    public class HRDashboardStats
    {
        public int TotalLecturers { get; set; }
        public int ActiveLecturers { get; set; }
        public int TotalClaims { get; set; }
        public int PendingClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public decimal TotalAmountApproved { get; set; }
    }

    public class ReportData
    {
        public int TotalClaims { get; set; }
        public decimal TotalAmount { get; set; }
        public int ApprovedClaims { get; set; }
        public int PendingClaims { get; set; }
        public int RejectedClaims { get; set; }
        public List<Claim> Claims { get; set; } = new List<Claim>();
    }

    public class MonthlyTrend
    {
        public DateTime Month { get; set; }
        public int TotalClaims { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
    }

    public class DepartmentBreakdown
    {
        public string Department { get; set; } = string.Empty;
        public int TotalClaims { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
    }

    public class LecturerPerformance
    {
        public string LecturerName { get; set; } = string.Empty;
        public int TotalClaims { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
    }

    public class StatusDistribution
    {
        public int Total { get; set; }
        public int Pending { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
    }

    public class ClaimAnalysis
    {
        public List<MonthlyTrend> MonthlyTrends { get; set; } = new List<MonthlyTrend>();
        public List<DepartmentBreakdown> DepartmentBreakdown { get; set; } = new List<DepartmentBreakdown>();
        public List<LecturerPerformance> TopPerformers { get; set; } = new List<LecturerPerformance>();
        public StatusDistribution StatusDistribution { get; set; } = new StatusDistribution();
    }
}