using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects.Entities
{
    public class MedicalService
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("Department")]
        public int DepartmentId { get; set; }
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty; 
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        [MaxLength(255)]
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public Department Department { get; set; } = null!;
        public ICollection<ServiceOrder> ServiceOrders { get; set; } = new List<ServiceOrder>();
    }
}
