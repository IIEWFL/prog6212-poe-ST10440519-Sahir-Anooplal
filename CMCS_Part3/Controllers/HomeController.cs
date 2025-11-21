using CMCS_Part3.Models;
using CMCS_Part3.Services;
using CMCS_Part3.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics; //[2]
using System.Security.Claims;

namespace CMCS_Part3.Controllers
{
    public class HomeController : Controller
    {
        private readonly IClaimService _claimService;
        private readonly IReportService _reportService;

        public HomeController(IClaimService claimService, IReportService reportService)
        {
            _claimService = claimService; //[4]
            _reportService = reportService; //[4]
        }

        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true) //[1]
            {
                return RedirectToAction("Dashboard");
            }
            return View();
        }

        [Authorize] //[1]
        public async Task<IActionResult> Dashboard()
        {
            var userRole = User.IsInRole("Lecturer") ? "Lecturer" :
                          User.IsInRole("ProgrammeCoordinator") ? "ProgrammeCoordinator" :
                          User.IsInRole("AcademicManager") ? "AcademicManager" : "HR";

            ViewBag.UserRole = userRole;

            if (userRole == "Lecturer")
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); //[3]
                if (!string.IsNullOrEmpty(userId))
                {
                    var claims = await _claimService.GetClaimsByLecturerAsync(userId);
                    return View("LecturerDashboard", claims);
                }
            }
            else if (userRole == "ProgrammeCoordinator" || userRole == "AcademicManager")
            {
                var pendingClaims = await _claimService.GetPendingClaimsAsync(); //[4]
                return View("ApproverDashboard", pendingClaims);
            }
            else if (userRole == "HR")
            {
                var dashboardData = new HRDashboardViewModel
                {
                    TotalApprovedClaims = await _reportService.GetApprovedClaimsCountAsync(),
                    TotalMonthlyAmount = await _reportService.GetTotalMonthlyAmountAsync(),
                    PendingPayments = await _reportService.GetPendingPaymentsCountAsync()
                };
                return View("HRDashboard", dashboardData);
            }

            // Default fallback
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}

/*
[1] Microsoft Docs. "ASP.NET Core Fundamentals." https://learn.microsoft.com/en-us/aspnet/core/
[2] Microsoft Docs. "System.Diagnostics Namespace." https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics
[3] Microsoft Docs. "Claims-based identity in ASP.NET Core." https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity#claims-based-identity
[4] Microsoft Docs. "Dependency Injection in ASP.NET Core." https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection
*/