using System.ComponentModel.DataAnnotations;

namespace AceJobAgency.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = null!;

        [Required]
        [MaxLength(10)]
        public string Gender { get; set; } = null!;

        [Required]
        public string EncryptedNRIC { get; set; } = null!;

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!;

        [Required]
        public DateTime DateOfBirth { get; set; }

        [MaxLength(500)]
        public string? ResumePath { get; set; }

        [MaxLength(1000)]
        public string? WhoAmI { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        public int FailedLoginAttempts { get; set; } = 0;

        public DateTime? LockoutEnd { get; set; }

        [MaxLength(100)]
        public string? SessionId { get; set; }

        public DateTime? LastPasswordChange { get; set; }
    }
}