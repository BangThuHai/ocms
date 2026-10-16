using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects.Entities
{
    public class Department
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(20)]
        public string Code { get; set; } = string.Empty; // INT, PED, ENT...
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(500)]
        public string? Description { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseConsultationFee { get; set; } = 150000;
        // Navigation Properties
        public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
        public ICollection<MedicalService> MedicalServices { get; set; } = new List<MedicalService>();
    }
}
