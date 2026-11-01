using BusinessObjects.Entities;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories
{
    public class TimeSlotRepository : ITimeSlotRepository
    {
        private readonly OcmsDbContext context;
        public TimeSlotRepository(OcmsDbContext context)
        {
            this.context = context;
        }

        public async Task<List<TimeSlot>> GetSlotsByDoctorAndDateAsync(int doctorId, DateTime targetDate)
        {
            var dateOnly = targetDate.Date;
            return await context.TimeSlots.Include(ts => ts.DoctorSchedule.DoctorId == doctorId && ts.DoctorSchedule.WorkDate == dateOnly && ts.DoctorSchedule.Status == "Active").OrderBy(ts => ts.StartTime)
                .ToListAsync();
        }

        public async Task<TimeSlot?> GetSlotByIdAsync(int slotId)
        {
            return await context.TimeSlots.Include(ts => ts.DoctorSchedule).ThenInclude(s => s.Doctor).ThenInclude(d => d.User).FirstOrDefaultAsync(ts => ts.Id == slotId);
        }

        public async Task<bool> HoldSlotAsync(int slotId, byte[] expectedRowVersion)
        {
            var slot = await context.TimeSlots.FirstOrDefaultAsync(ts => ts.Id == slotId);
            if (slot == null || slot.Status != "Open")
            {
                return false;
            }

            /*
             UPDATE [Slots]
            SET [IsBooked] = @Current_IsBooked, ...
            WHERE [SlotId] = @SlotId AND [RowVersion] = @Original_RowVersion;
             */
             context.Entry(slot).Property(s => s.RowVersion).OriginalValue = expectedRowVersion;


            slot.Status = "Hold";
            try
            {
                await context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                return false;
            }
        }
        public async Task<bool> ReleaseSlotAsync(int slotId)
        {
            var slot = await context.TimeSlots.FirstOrDefaultAsync(ts => ts.Id == slotId);
            if (slot == null)
            {
                return false;
            }

            if (slot.Status == "Hold")
            {
                slot.Status = "Open";
                await context.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}
