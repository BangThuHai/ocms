using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects.Entities
{
    public class Doctor
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("User")]
        public int UserId { get; set; }
        [ForeignKey("Department")]
        public int DepartmentId { get; set; }
        [ForeignKey("Room")]
        public int? DefaultRoomId { get; set; }
        [MaxLength(50)]
        public string AcademicTitle { get; set; } = "BS"; // BS, ThS.BS, CK1...

        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerSlot { get; set; } = 150000;

        // Navigation Properties
        public User User { get; set; } = null!;
        public Department Department { get; set; } = null!;
        public Room? DefaultRoom { get; set; }
        public ICollection<DoctorSchedule> Schedules { get; set; } = new List<DoctorSchedule>();
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    }
}
