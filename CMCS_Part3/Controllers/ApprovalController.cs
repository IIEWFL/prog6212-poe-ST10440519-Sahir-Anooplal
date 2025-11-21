using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CMCS_Part3.Models;
using CMCS_Part3.Services;

namespace CMCS_Part3.Controllers
{
    [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")] //[1]
    public class ApprovalController : Controller
    {
        private readonly IClaimService _claimService; //[2]
        private readonly IReportService _reportService; //[2]

        public ApprovalController(IClaimService claimService, IReportService reportService)
        {
            _claimService = claimService; //[2]
            _reportService = reportService; //[2]
        }

        public async Task<IActionResult> PendingClaims()
        {
            var claims = await _claimService.GetPendingClaimsAsync(); //[2]
            return View(claims);
        }

        public async Task<IActionResult> ApprovalHistory()
        {
            var claims = await _claimService.GetAllClaimsAsync(); //[2]
            return View(claims);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] //[1]
        public async Task<IActionResult> Approve(int claimId)
        {
            try
            {
                var currentUser = User.Identity?.Name ?? "Unknown"; //[1]
                var result = await _claimService.ApproveClaimAsync(claimId, currentUser);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Claim approved successfully!"; //[1]
                }
                else
                {
                    TempData["ErrorMessage"] = result.ErrorMessage; //[1]
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while approving the claim."; //[1]
            }

            return RedirectToAction("PendingClaims"); //[1]
        }

        [HttpPost]
        [ValidateAntiForgeryToken] //[1]
        public async Task<IActionResult> Reject(int claimId, string rejectionReason)
        {
            try
            {
                var currentUser = User.Identity?.Name ?? "Unknown"; //[1]
                var result = await _claimService.RejectClaimAsync(claimId, currentUser, rejectionReason);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Claim rejected successfully!"; //[1]
                }
                else
                {
                    TempData["ErrorMessage"] = result.ErrorMessage; //[1]
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while rejecting the claim."; //[1]
            }

            return RedirectToAction("PendingClaims"); //[1]
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var claim = await _claimService.GetClaimByIdAsync(id); //[2]
            if (claim == null)
            {
                return NotFound();
            }

            return View(claim);
        }
    }
}

/*
[1] Microsoft Docs. "ASP.NET Core Fundamentals." https://learn.microsoft.com/en-us/aspnet/core/
[2] Microsoft Docs. "Dependency Injection in ASP.NET Core." https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection
*/