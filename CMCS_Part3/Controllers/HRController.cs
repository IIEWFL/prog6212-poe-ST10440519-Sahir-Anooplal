using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CMCS_Part3.Models;
using CMCS_Part3.Services;
using CMCS_Part3.ViewModels;

namespace CMCS_Part3.Controllers
{
    [Authorize(Roles = "HR")] //[1]
    public class HRController : Controller
    {
        private readonly IReportService _reportService; //[2]
        private readonly IClaimService _claimService; //[2]
        private readonly IUserService _userService; //[2]

        public HRController(IReportService reportService, IClaimService claimService, IUserService userService)
        {
            _reportService = reportService; //[2]
            _claimService = claimService; //[2]
            _userService = userService; //[2]
        }

        public async Task<IActionResult> Dashboard()
        {
            var dashboardData = new HRDashboardViewModel
            {
                TotalApprovedClaims = await _reportService.GetApprovedClaimsCountAsync(), //[2]
                TotalMonthlyAmount = await _reportService.GetTotalMonthlyAmountAsync(), //[2]
                PendingPayments = await _reportService.GetPendingPaymentsCountAsync() //[2]
            };

            return View(dashboardData);
        }

        public async Task<IActionResult> Reports()
        {
            var currentMonth = DateTime.UtcNow.Month; //[1]
            var currentYear = DateTime.UtcNow.Year; //[1]

            var reports = await _reportService.GetMonthlyReportAsync(currentMonth, currentYear); //[2]
            return View(reports);
        }

        [HttpPost]
        public async Task<IActionResult> GenerateReport(int month, int year)
        {
            var reports = await _reportService.GetMonthlyReportAsync(month, year); //[2]
            return View("Reports", reports);
        }

        public async Task<IActionResult> ManageLecturers()
        {
            var lecturers = await _reportService.GetAllLecturersAsync(); //[2]
            return View(lecturers);
        }

        public async Task<IActionResult> AllClaims()
        {
            var claims = await _claimService.GetAllClaimsAsync(); //[2]
            return View(claims);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] //[1]
        public async Task<IActionResult> MarkAsPaid(int claimId)
        {
            try
            {
                var currentUser = User.Identity?.Name ?? "Unknown"; //[1]
                var result = await _claimService.UpdateClaimStatusAsync(claimId, ClaimStatus.Paid, currentUser); //[2]

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Claim marked as paid successfully!"; //[1]
                }
                else
                {
                    TempData["ErrorMessage"] = result.ErrorMessage; //[1]
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while updating the claim status."; //[1]
            }

            return RedirectToAction("AllClaims");
        }
    }
}

/*
[1] Microsoft Docs. "ASP.NET Core Fundamentals." https://learn.microsoft.com/en-us/aspnet/core/
[2] Microsoft Docs. "Dependency Injection in ASP.NET Core." https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection
*/