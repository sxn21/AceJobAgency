namespace AceJobAgency.Services
{
    public class PasswordService : IPasswordService
    {
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, 12);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch
            {
                return false;
            }
        }

        public (bool isValid, string message) ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return (false, "Password is required");

            if (password.Length < 12)
                return (false, "Password must be at least 12 characters");

            bool hasLower = password.Any(char.IsLower);
            bool hasUpper = password.Any(char.IsUpper);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

            if (!hasLower)
                return (false, "Password must contain at least one lowercase letter");
            if (!hasUpper)
                return (false, "Password must contain at least one uppercase letter");
            if (!hasDigit)
                return (false, "Password must contain at least one number");
            if (!hasSpecial)
                return (false, "Password must contain at least one special character");

            return (true, "Password is strong");
        }
    }
}