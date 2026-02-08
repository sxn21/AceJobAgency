using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AceJobAgency.Models;
using AceJobAgency.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AceJobAgency.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly IEncryptionService _encryptionService;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public AccountController(
            ApplicationDbContext context,
            IPasswordService passwordService,
            IEncryptionService encryptionService,
            IWebHostEnvironment environment,
            IConfiguration configuration)
        {
            _context = context;
            _passwordService = passwordService;
            _encryptionService = encryptionService;
            _environment = environment;
            _configuration = configuration;
        }

        // GET: Register
        [HttpGet]
        public IActionResult Register()
        {
            if (SessionHelper.IsAuthenticated(HttpContext))
            {
                return RedirectToAction("Index", "Home");
            }

            // Pass the reCAPTCHA site key to the view
            ViewData["RecaptchaSiteKey"] = _configuration["Recaptcha:SiteKey"];

            return View();
        }


        // POST: Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Verify reCAPTCHA
            if (!await VerifyRecaptcha(model.RecaptchaToken))
            {
                ModelState.AddModelError("", "reCAPTCHA verification failed. Please try again.");
                return View(model);
            }

            // Server-side password validation
            var (isValid, message) = _passwordService.ValidatePassword(model.Password);
            if (!isValid)
            {
                ModelState.AddModelError("Password", message);
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check for duplicate email
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "This email is already registered");
                return View(model);
            }

            // Handle file upload
            string? resumePath = null;
            if (model.Resume != null)
            {
                var uploadResult = await HandleFileUpload(model.Resume);
                if (!uploadResult.success)
                {
                    ModelState.AddModelError("Resume", uploadResult.message);
                    return View(model);
                }
                resumePath = uploadResult.path;
            }

            // Create user
            var user = new User
            {
                FirstName = System.Net.WebUtility.HtmlEncode(model.FirstName),
                LastName = System.Net.WebUtility.HtmlEncode(model.LastName),
                Gender = model.Gender,
                EncryptedNRIC = _encryptionService.Encrypt(model.NRIC),
                Email = model.Email.ToLower(),
                PasswordHash = _passwordService.HashPassword(model.Password),
                DateOfBirth = model.DateOfBirth,
                ResumePath = resumePath,
                WhoAmI = System.Net.WebUtility.HtmlEncode(model.WhoAmI),
                CreatedAt = DateTime.UtcNow,
                LastPasswordChange = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Save password history
            var passwordHistory = new PasswordHistory
            {
                UserId = user.UserId,
                PasswordHash = user.PasswordHash,
                ChangedAt = DateTime.UtcNow
            };
            _context.PasswordHistories.Add(passwordHistory);
            await _context.SaveChangesAsync();

            // Log audit
            await LogAuditAsync(user.UserId, "User registered", user.Email);

            TempData["SuccessMessage"] = "Registration successful! Please login.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (SessionHelper.IsAuthenticated(HttpContext))
            {
                return RedirectToAction("Index", "Home");
            }

            // Pass the site key to the view
            ViewData["RecaptchaSiteKey"] = _configuration["Recaptcha:SiteKey"];
            return View();
        }


        // POST: Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // Verify reCAPTCHA
            if (!await VerifyRecaptcha(model.RecaptchaToken))
            {
                ModelState.AddModelError("", "reCAPTCHA verification failed. Please try again.");
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email.ToLower());

            if (user == null)
            {
                await LogAuditAsync(0, "Failed login - user not found", model.Email);
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }

            // Check if account is locked
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            {
                var remainingTime = Math.Ceiling((user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes);
                await LogAuditAsync(user.UserId, "Login attempt on locked account", model.Email);
                ModelState.AddModelError("", $"Account is locked. Please try again in {remainingTime} minute(s).");
                return View(model);
            }

            // Verify password
            if (!_passwordService.VerifyPassword(model.Password, user.PasswordHash))
            {
                user.FailedLoginAttempts++;

                if (user.FailedLoginAttempts >= 3)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                    await _context.SaveChangesAsync();
                    await LogAuditAsync(user.UserId, "Account locked due to failed attempts", model.Email);
                    ModelState.AddModelError("", "Account locked for 15 minutes due to multiple failed login attempts.");
                }
                else
                {
                    await _context.SaveChangesAsync();
                    await LogAuditAsync(user.UserId, "Failed login attempt", model.Email);
                    ModelState.AddModelError("", $"Invalid email or password. {3 - user.FailedLoginAttempts} attempt(s) remaining.");
                }

                return View(model);
            }

            // Check for multiple login
            string newSessionId = Guid.NewGuid().ToString();
            if (!string.IsNullOrEmpty(user.SessionId))
            {
                await LogAuditAsync(user.UserId, "Multiple login detected - previous session invalidated", model.Email);
            }

            // Successful login
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            user.LastLoginAt = DateTime.UtcNow;
            user.SessionId = newSessionId;
            await _context.SaveChangesAsync();

            // Create session with the same session ID
            SessionHelper.SetSession(HttpContext, user.UserId, user.Email, newSessionId);

            await LogAuditAsync(user.UserId, "Successful login", model.Email);

            return RedirectToAction("Index", "Home");
        }

        // Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userId = SessionHelper.GetUserId(HttpContext);
            var email = SessionHelper.GetEmail(HttpContext);

            if (userId.HasValue)
            {
                var user = await _context.Users.FindAsync(userId.Value);
                if (user != null)
                {
                    user.SessionId = null;
                    await _context.SaveChangesAsync();
                }

                await LogAuditAsync(userId.Value, "User logged out", email);
            }

            SessionHelper.ClearSession(HttpContext);
            return RedirectToAction("Login");
        }

        // File upload handler
        private async Task<(bool success, string message, string? path)> HandleFileUpload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return (false, "Please select a file", null);
            }

            // Check file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                return (false, "File size must be less than 5MB", null);
            }

            // Check file extension
            var allowedExtensions = new[] { ".pdf", ".docx" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return (false, "Only .pdf and .docx files are allowed", null);
            }

            // Generate unique filename
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "resumes");
            Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return (true, "File uploaded successfully", $"/uploads/resumes/{uniqueFileName}");
        }

        // reCAPTCHA verification
        private async Task<bool> VerifyRecaptcha(string? token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            var secretKey = _configuration["Recaptcha:SecretKey"];

            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.PostAsync(
                        $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={token}",
                        null);

                    var jsonString = await response.Content.ReadAsStringAsync();
                    
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    var result = JsonSerializer.Deserialize<RecaptchaResponse>(jsonString, options);

                    return result?.Success == true && result?.Score >= 0.5;
                }
                catch
                {
                    return false;
                }
            }
        }

        // Audit logging
        private async Task LogAuditAsync(int userId, string action, string? email)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                Timestamp = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                Email = email
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
    }

    // Helper class for reCAPTCHA response
    public class RecaptchaResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("score")]
        public double Score { get; set; }
    }
}