using BusinessObjects.Entities;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories
{
    public class AppointmentRepository : IAppointmentRepository
    {

        private readonly OcmsDbContext context;
        public AppointmentRepository(OcmsDbContext context)
        {
            this.context = context;
        }
        public async Task<Appointment> CreateAppointmentAsync(int patientId, int slotId, string? reason)
        {
            var slot = await context.TimeSlots.Include(ts => ts.DoctorSchedule).FirstOrDefaultAsync(ts => ts.Id == slotId);

            if(slot ==null){
                throw new InvalidOperationException("Khung giờ không tồn tại.");
            }

            slot.Status = "Booked";

            string qrToken = $"QR-{slot.DoctorSchedule.WorkDate:yyyyMMdd}-{Guid.NewGuid():N}".Substring(0, 20).ToUpper();//Guid.NewGuid: Tạo một chuỗi duy nhất

            var appointment = new Appointment
            {
                PatientId = patientId,
                DoctorId = slot.DoctorSchedule.DoctorId,
                TimeSlotId = slotId,
                AppointmentDate = slot.DoctorSchedule.WorkDate,
                Status = "Confirmed",
                Reason = reason,
                QrCodeToken = qrToken,
                RescheduledCount = 0,
                CreatedAt = DateTime.UtcNow,
            };

            context.Appointments.Add(appointment);
            await context.SaveChangesAsync();
            return appointment;
        }

        public async Task<Appointment?> GetAppointmentDetailsAsync(int appointmentId)
        {
            return await context.Appointments.Include(a => a.Patient).ThenInclude(p => p.User)
                                             .Include(a => a.Doctor).ThenInclude(d => d.User)
                                             .Include(a => a.Doctor).ThenInclude(d => d.Department)
                                             .Include(a => a.TimeSlot).ThenInclude(ts => ts.DoctorSchedule).ThenInclude(s => s.Room)
                                             .FirstOrDefaultAsync(a => a.Id == appointmentId);
        }

        public async Task<Patient?> GetPatientByUserIdAsync(int userId)
        {
            return await context.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.UserId == userId);
        }
    }
}
