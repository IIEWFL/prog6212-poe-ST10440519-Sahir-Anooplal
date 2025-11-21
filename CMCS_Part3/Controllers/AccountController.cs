using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using CMCS_Part3.Models;
using CMCS_Part3.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace CMCS_Part3.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager; //[2]
        private readonly UserManager<ApplicationUser> _userManager; //[2]
        private readonly ILogger<AccountController> _logger; //[1]

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager; //[2]
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous] //[1]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl; //[1]
            return View();
        }

        [HttpPost]
        [AllowAnonymous] //[1]
        [ValidateAntiForgeryToken] //[1]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false); //[2]
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return RedirectToLocal(returnUrl); //[1]
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model); //[1]
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] //[1]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync(); //[2]
            _logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpGet]
        [Authorize(Roles = "HR")] //[1]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "HR")] //[1]
        [ValidateAntiForgeryToken] //[1]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid) //[1]
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Role = model.Role
                };

                var result = await _userManager.CreateAsync(user, model.Password); //[2]
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, model.Role); //[2]

                    _logger.LogInformation("User created a new account with password.");

                    TempData["SuccessMessage"] = "User registered successfully!"; //[1]
                    return RedirectToAction("ManageLecturers", "HR"); //[1]
                }
                foreach (var error in result.Errors) //[2]
                {
                    ModelState.AddModelError(string.Empty, error.Description); //[1]
                }
            }

            return View(model);
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl)) //[1]
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Dashboard), "Home"); //[1]
            }
        }
    }
}

/*
[1] Microsoft Docs. "ASP.NET Core Fundamentals." https://learn.microsoft.com/en-us/aspnet/core/
[2] Microsoft Docs. "ASP.NET Core Identity." https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity
*/