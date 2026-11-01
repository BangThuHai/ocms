using BusinessObjects.Entities;
using Services;
using System.ComponentModel.DataAnnotations;

namespace OCMSMVC.Models
{
    public class DoctorSearchViewModel
    {
        public int? SelectedDepartmentId { get; set; }
        public string? SearchTerm { get; set; }
        public DateTime? TargetDate { get; set; } = DateTime.Today;

        public List<Department> Departments { get; set; } = new();
        public List<DoctorCardItem> DoctorCards { get; set; } = new();

    }
    public class DoctorCardItem
    {
        public int DoctorId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string AcademicTitle { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public decimal PricePerSlot { get; set; }
        public double AverageRating { get; set; } = 5.0;
        public int TotalReviews { get; set; }

        public ShiftCapacityMetrics MorningCapacity { get; set; } = new ShiftCapacityMetrics();
        public ShiftCapacityMetrics AfternoonCapacity { get; set; } = new ShiftCapacityMetrics();
    }

    public class SelectSlotViewModel
    {
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string AcademicTitle { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public decimal PricePerSlot { get; set; }
        public DateTime SelectedDate { get; set; }
        public List<SlotGridItem> MorningSlots { get; set; } = new();
        public List<SlotGridItem> AfternoonSlots { get; set; } = new();
        public ShiftCapacityMetrics DayCapacity { get; set; } = new();
    }

    public class SlotGridItem
    {
        public int SlotId { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Status { get; set; } = "Open";    // Open, Hold, Booked
        public string RowVersion { get; set; } = string.Empty;
        public bool IsAvailable => Status == "Open";


    }

    public class HoldSlotPostModel
    {
        [Required]
        public int SlotId { get; set; }
        [Required]
        public string RowVersion { get; set; } = string.Empty;
    }
}
