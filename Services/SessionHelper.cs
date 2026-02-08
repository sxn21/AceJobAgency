using Microsoft.AspNetCore.Http;

namespace AceJobAgency.Services
{
    public static class SessionHelper
    {
        public static void SetSession(HttpContext context, int userId, string email, string? sessionId = null)
        {
            string session = sessionId ?? Guid.NewGuid().ToString();

            context.Session.SetInt32("UserId", userId);
            context.Session.SetString("Email", email);
            context.Session.SetString("SessionId", session);
            context.Session.SetString("LoginTime", DateTime.UtcNow.ToString("o"));
        }

        public static int? GetUserId(HttpContext context)
        {
            return context.Session.GetInt32("UserId");
        }

        public static string? GetEmail(HttpContext context)
        {
            return context.Session.GetString("Email");
        }

        public static string? GetSessionId(HttpContext context)
        {
            return context.Session.GetString("SessionId");
        }

        public static bool IsAuthenticated(HttpContext context)
        {
            return context.Session.GetInt32("UserId").HasValue;
        }

        public static void ClearSession(HttpContext context)
        {
            context.Session.Clear();
        }
    }
}