namespace AceJobAgency.Services
{
    public interface IPasswordService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
        (bool isValid, string message) ValidatePassword(string password);
    }
}