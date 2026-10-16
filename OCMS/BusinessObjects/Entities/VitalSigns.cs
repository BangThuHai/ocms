using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects.Entities
{
    public class VitalSigns
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("Appointment")]
        public int AppointmentId { get; set; }
        public int SystolicBP { get; set; } // Huyết áp tâm thu (e.g. 120)
        public int DiastolicBP { get; set; } // Huyết áp tâm trương (e.g. 80)
        public int Pulse { get; set; } // Mạch (e.g. 75 bpm)

        [Column(TypeName = "decimal(4,1)")]
        public decimal Temperature { get; set; } // Nhiệt độ (e.g. 36.5)
        public int SpO2 { get; set; } // Nồng độ oxy (e.g. 98%)

        [Column(TypeName = "decimal(5,2)")]
        public decimal Weight { get; set; } // Cân nặng kg

        [Column(TypeName = "decimal(5,2)")]
        public decimal Height { get; set; } // Chiều cao cm
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
        // Navigation Properties
        public Appointment Appointment { get; set; } = null!;
    }
}
