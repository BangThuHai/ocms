using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;
        [Required, MaxLength(100), EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required, MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        [Required, MaxLength(20)]
        public string Role { get; set; } = "Patient"; // Patient, Doctor, Staff, Admin
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        //Navigation properties
        public Patient? Patient { get; set; }
        public Doctor? Doctor { get; set; }

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
