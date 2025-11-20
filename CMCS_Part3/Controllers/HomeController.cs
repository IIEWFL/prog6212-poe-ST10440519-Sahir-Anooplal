using Microsoft.AspNetCore.Mvc;
using CMCS_Part3.Services;
using CMCS_Part3.Models;

namespace CMCS_Part3.Controllers
{
    public class HomeController : Controller
    {
        private readonly IUserService _userService;

        public HomeController(IUserService userService)
        {
            _userService = userService;
        }

        public IActionResult Index()
        {
            var currentUser = _userService.GetCurrentUser();
            if (currentUser == null)
            {
                return RedirectToAction("SelectRole");
            }

            ViewData["CurrentUser"] = currentUser;
            return View();
        }

        public IActionResult SelectRole()
        {
            var lecturers = _userService.GetAllLecturers();
            ViewBag.Lecturers = lecturers;
            return View();
        }

        [HttpPost]
        public IActionResult SelectRole(int lecturerId, string role)
        {
            if (lecturerId > 0 && !string.IsNullOrEmpty(role))
            {
                _userService.SetCurrentUser(lecturerId, role);
                return RedirectToAction("Index");
            }

            TempData["ErrorMessage"] = "Please select both a lecturer and a role.";
            return RedirectToAction("SelectRole");
        }

        public IActionResult LoginAsApprover()
        {
            // Direct login for approvers 
            _userService.SetCurrentUser(0, UserRole.ProgrammeCoordinator);
            return RedirectToAction("Index");
        }

        public IActionResult LoginAsHR()
        {
            // Direct login for HR
            _userService.SetCurrentUser(0, UserRole.HR);
            return RedirectToAction("Index");
        }

        public IActionResult Logout()
        {
            _userService.ClearCurrentUser();
            return RedirectToAction("SelectRole");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}