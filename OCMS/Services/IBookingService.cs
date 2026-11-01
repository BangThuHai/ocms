using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class HoldSlotResult{
        public bool IsSuccess { get; set; }
        public int SlotId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public int ExpiresInSeconds { get; set; } = 600; // Default to 10 minutes
        public List<AlternativeSlotDto> AlternativeSlots { get; set; } = new();
    }

    public class AlternativeSlotDto{
    public int Id { get; set; }
    public string TimeRange { get; set; }
        public string RowVersion { get; set; } = string.Empty;
    }

    public interface IBookingService
    {

    }
}
