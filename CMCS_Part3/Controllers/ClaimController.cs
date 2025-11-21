using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CMCS_Part3.Models;
using CMCS_Part3.Services;
using CMCS_Part3.ViewModels;
using System.Security.Claims;

namespace CMCS_Part3.Controllers
{
    [Authorize(Roles = "Lecturer")] //[1]
    public class ClaimController : Controller
    {
        private readonly IClaimService _claimService; //[2]
        private readonly IWebHostEnvironment _environment; //[1]

        public ClaimController(IClaimService claimService, IWebHostEnvironment environment)
        {
            _claimService = claimService; //[2]
            _environment = environment; //[1]
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); //[1]
            if (!string.IsNullOrEmpty(userId))
            {
                var userClaims = await _claimService.GetClaimsByLecturerAsync(userId); //[2]
                return View(userClaims);
            }
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult Submit()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken] //[1]
        public async Task<IActionResult> Submit(ClaimViewModel model)
        {
            if (ModelState.IsValid) //[1]
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); //[1]
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account"); //[1]
                }

                var claim = new CMCS_Part3.Models.Claim
                {
                    LecturerId = userId,
                    HoursWorked = model.HoursWorked,
                    HourlyRate = model.HourlyRate,
                    AdditionalNotes = model.AdditionalNotes,
                    Status = ClaimStatus.Pending,
                    SubmittedDate = DateTime.UtcNow
                };

                var result = await _claimService.SubmitClaimAsync(claim, model.SupportingDocument);

                if (result.Success) //[2]
                {
                    TempData["SuccessMessage"] = "Claim submitted successfully!"; //[1]
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", result.ErrorMessage); //[1]
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> MyClaims()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); //[1]
            if (!string.IsNullOrEmpty(userId))
            {
                var claims = await _claimService.GetClaimsByLecturerAsync(userId); //[2]
                return View(claims);
            }
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var claim = await _claimService.GetClaimByIdAsync(id); //[2]
            if (claim == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); //[1]

            // Ensure user can only access their own claims unless they are approvers or HR
            if (claim.LecturerId != userId && !User.IsInRole("ProgrammeCoordinator") && !User.IsInRole("AcademicManager") && !User.IsInRole("HR")) //[1]
            {
                return Forbid();
            }

            return View(claim);
        }
    }
}

/*
[1] Microsoft Docs. "ASP.NET Core Fundamentals." https://learn.microsoft.com/en-us/aspnet/core/
[2] Microsoft Docs. "Dependency Injection in ASP.NET Core." https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection
*/