using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects.Entities
{
    public class DoctorSchedule
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("Doctor")]
        public int DoctorId { get; set; }
        [ForeignKey("Room")]
        public int RoomId { get; set; }
        public DateTime WorkDate { get; set; }
        [Required, MaxLength(20)]
        public string ShiftType { get; set; } = "Morning"; // Morning (07:30-11:30), Afternoon (13:00-17:00)
        public int MaxQuota { get; set; } = 16;
        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // Active, Cancelled
        // Navigation Properties
        public Doctor Doctor { get; set; } = null!;
        public Room Room { get; set; } = null!;
        public ICollection<TimeSlot> TimeSlots { get; set; } = new List<TimeSlot>();
    }
}
