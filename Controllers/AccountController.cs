using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebstackInfrar.Models;
using WebstackInfrar.ViewModels;

namespace WebstackInfrar.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signIn;
        private readonly UserManager<ApplicationUser> _users;

        public AccountController(SignInManager<ApplicationUser> signIn,
                                 UserManager<ApplicationUser> users)
        {
            _signIn = signIn;
            _users = users;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _users.FindByEmailAsync(model.Email);
            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError("", "Invalid credentials or account is inactive.");
                return View(model);
            }

            var result = await _signIn.PasswordSignInAsync(user, model.Password,
                                                            model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                if (await _users.IsInRoleAsync(user, "Admin"))
                    return RedirectToLocal(returnUrl, "/Admin/Dashboard");
                return RedirectToLocal(returnUrl, "/Employee/Dashboard");
            }

            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        [HttpPost, Authorize, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signIn.SignOutAsync();
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied() => View();

        private IActionResult RedirectToLocal(string? returnUrl, string fallback)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return Redirect(fallback);
        }
    }
}