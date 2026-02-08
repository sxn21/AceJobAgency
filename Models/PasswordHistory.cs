using System.ComponentModel.DataAnnotations;

namespace AceJobAgency.Models
{
    public class PasswordHistory
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [Required]
        public string PasswordHash { get; set; } = null!;

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}