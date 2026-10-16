using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects.Entities
{
    public class Patient
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("User")]
        public int UserId { get; set; }
        public DateTime DateOfBirth { get; set; }
        [MaxLength(10)]
        public string Gender { get; set; } = "Other"; // Male, Female, Other
        [MaxLength(255)]
        public string? Address { get; set; }
        [MaxLength(50)]
        public string? HealthInsuranceNumber { get; set; }
        // Navigation Properties
        public User User { get; set; } = null!;
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    }
}
