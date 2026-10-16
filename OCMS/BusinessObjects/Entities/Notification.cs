using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects.Entities
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("User")]
        public int UserId { get; set; }
        [Required, MaxLength(500)]
        public string Message { get; set; } = string.Empty;
        [Required, MaxLength(30)]
        public string NotificationType { get; set; } = "Reminder"; // Reminder, System, OTP
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // Navigation Properties
        public User User { get; set; } = null!;
    }
}
