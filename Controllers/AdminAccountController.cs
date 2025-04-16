using FoodDeliveryApp.Data;
using FoodDeliveryApp.ViewModels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FoodDeliveryApp.Controllers
{
    public class AdminAccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminAccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /AdminAccount/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /AdminAccount/Login
        [HttpPost]
        public async Task<IActionResult> Login(AdminLoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var admin = _context.Admins
                .FirstOrDefault(a => a.Email == model.Email && a.Password == model.Password);

            if (admin == null)
            {
                ModelState.AddModelError("", "Invalid Admin Credentials.");
                return View(model);
            }

            // Build admin claims (e.g., email, role, etc.)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, admin.Name),
                new Claim(ClaimTypes.Email, admin.Email),
                new Claim("AdminId", admin.AdminId.ToString()),
                // Optionally, set role for role-based checks
                new Claim(ClaimTypes.Role, "Admin")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(claimsIdentity);

            // Sign in: create the auth cookie
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            // Redirect to admin area
            return RedirectToAction("Orders", "Admin");
        }

        public async Task<IActionResult> Logout()
        {
            // Sign out: remove the auth cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}