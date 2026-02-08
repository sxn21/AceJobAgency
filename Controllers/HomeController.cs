using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AceJobAgency.Models;
using AceJobAgency.Services;

namespace AceJobAgency.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEncryptionService _encryptionService;

        public HomeController(ApplicationDbContext context, IEncryptionService encryptionService)
        {
            _context = context;
            _encryptionService = encryptionService;
        }

        public async Task<IActionResult> Index()
        {
            // Check if user is authenticated
            if (!SessionHelper.IsAuthenticated(HttpContext))
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = SessionHelper.GetUserId(HttpContext);
            var sessionId = SessionHelper.GetSessionId(HttpContext);

            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(userId.Value);

            if (user == null)
            {
                SessionHelper.ClearSession(HttpContext);
                return RedirectToAction("Login", "Account");
            }

            // Check for multiple logins
            if (user.SessionId != sessionId)
            {
                SessionHelper.ClearSession(HttpContext);
                TempData["ErrorMessage"] = "Your session has been terminated due to login from another device.";
                return RedirectToAction("Login", "Account");
            }

            // Decrypt sensitive data for display
            user.EncryptedNRIC = _encryptionService.Decrypt(user.EncryptedNRIC);

            return View(user);
        }
    }
}