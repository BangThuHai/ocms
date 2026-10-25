using Microsoft.AspNetCore.Mvc;
using OCMSMVC.Models;
using Services;

namespace OCMSMVC.Controllers
{
    public class AuthController : Controller
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("Role")))
            {
                return RedirectToRoleDashboard();
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userService.AuthenticateAsync(model.Email, model.Password);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Email or password is incorrect.");
                return View(model);
            }

            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("Email", user.Email);
            HttpContext.Session.SetString("Role", user.Role);
            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                HttpContext.Session.SetString("PhoneNumber", user.PhoneNumber);
            }
            return RedirectToRoleDashboard(user.Role);
        }


        [HttpGet]
        public IActionResult Register()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("Role")))
            {
                return RedirectToRoleDashboard();
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _userService.RegisterPatientAsync(model.FullName, model.Email, model.PhoneNumber, model.Password);

                TempData["SuccessMessage"] = "Registration successful! Please log in.";
                return RedirectToAction("Login");
            }
            catch (InvalidOperationException e)
            {
                ModelState.AddModelError(string.Empty, e.Message);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }


        [HttpGet]
        public IActionResult AccessDenied(){
            return View();
        }

        private IActionResult RedirectToRoleDashboard(string? role = null){
            role ??= HttpContext.Session.GetString("Role");

            return role switch
            { 
                "Admin" => RedirectToAction("Index", "Home"),
                "Doctor" => RedirectToAction("Index", "Home"),
                "Staff" => RedirectToAction("Index", "Home"),
                _ => RedirectToAction("Index", "Home"),
            };
        }
    }

}
