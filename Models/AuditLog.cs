using System.ComponentModel.DataAnnotations;

namespace AceJobAgency.Models
{
    public class AuditLog
    {
        [Key]
        public int LogId { get; set; }

        public int UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Action { get; set; } = null!;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        [MaxLength(255)]
        public string? Email { get; set; }
    }
}