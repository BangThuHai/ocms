using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects.Entities
{
    public class Medicine
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty; // Panadol, Amoxicillin...
        [Required, MaxLength(20)]
        public string Unit { get; set; } = "Viên"; // Viên, Gói, Chai...
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int StockQuantity { get; set; } = 100;
        [MaxLength(255)]
        public string? Description { get; set; }

        public bool IsAvailable { get; set; } = true;

        // Navigation Properties
        public ICollection<PrescriptionDetail> PrescriptionDetails { get; set; } = new List<PrescriptionDetail>();
    }
}
