using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects.Entities
{
    public class Room
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(20)]
        public string RoomNumber { get; set; } = string.Empty; // 101, 201...
        [ForeignKey("Department")]
        public int DepartmentId { get; set; }
        [MaxLength(50)]
        public string RoomType { get; set; } = "Consultation"; // Consultation, Ultrasound, Lab
        public bool IsOperational { get; set; } = true;
        // Navigation Properties
        public Department Department { get; set; } = null!;
        public ICollection<DoctorSchedule> Schedules { get; set; } = new List<DoctorSchedule>();
    }
}
