using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects.Entities
{
    public class MedicalRecord
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("Appointment")]
        public int AppointmentId { get; set; }
        [Required, MaxLength(500)]
        public string Diagnosis { get; set; } = string.Empty;
        [MaxLength(20)]
        public string? ICD10Code { get; set; } // J00, I10...
        [MaxLength(1000)]
        public string? DoctorNotes { get; set; }
        public DateTime? FollowUpDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // Navigation Properties
        public Appointment Appointment { get; set; } = null!;
        public ICollection<PrescriptionDetail> PrescriptionDetails { get; set; } = new List<PrescriptionDetail>();
    }
}
