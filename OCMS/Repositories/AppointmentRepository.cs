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

            if (slot == null)
            {
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

        public async Task<Appointment?> GetAppointmentByIdAndPatientAsync(int appointmentId, int patientId)
        {
            return await context.Appointments.Include(a => a.Patient).ThenInclude(p => p.User)
                                            .Include(a => a.Doctor).ThenInclude(d => d.User)
                                            .Include(a => a.Doctor).ThenInclude(d => d.Department)
                                            .Include(a => a.Doctor).ThenInclude(d => d.DefaultRoom)
                                            .Include(a => a.TimeSlot).ThenInclude(ts => ts.DoctorSchedule).ThenInclude(s => s.Room)
                                            .Include(a => a.Payments)
                                            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.PatientId == patientId);
        }

        public async Task<List<Appointment>> GetAppointmentsByPatientIdAsync(int patientId)
        {
            return await context.Appointments.Include(a => a.Doctor).ThenInclude(d => d.User)
                                            .Include(a => a.Doctor).ThenInclude(d => d.Department)
                                            .Include(a => a.Doctor).ThenInclude(d => d.DefaultRoom)
                                            .Include(a => a.TimeSlot).ThenInclude(ts => ts.DoctorSchedule).ThenInclude(s => s.Room)
                                            .Include(a => a.Payments)
                                            .Where(a => a.PatientId == patientId)
                                            .OrderByDescending(a => a.AppointmentDate)
                                            .ThenByDescending(a => a.TimeSlot.StartTime)
                                            .ToListAsync();
        }

        public async Task<(bool Success, string Message, bool IsRefunded)> CancelAppointmentAsync(int appointmentId, int patientId, string? cancellationReason)
        {
            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var appt = await context.Appointments.Include(a => a.TimeSlot).Include(a => a.Payments).FirstOrDefaultAsync(a => a.Id == appointmentId && a.PatientId == patientId);

                if (appt == null)
                {
                    return (false, "Cuộc hẹn không tồn tại.", false);
                }

                if (appt.Status == "Cancelled")
                {
                    return (false, "Cuộc hẹn đã bị hủy trước đó.", false);
                }

                if (appt.Status == "Completed" || appt.Status == "InConsultation" || appt.Status == "Arrived")
                {
                    return (false, $"Không thể hủy lịch hẹn đang ở trạng thái '{appt.Status}'.", false);
                }

                var appointmentDateTime = appt.AppointmentDate.Date.Add(appt.TimeSlot.StartTime);
                var hoursRemaining = (appointmentDateTime - DateTime.Now).TotalHours;

                bool isRefunded = false;
                string refundNotice;

                if (hoursRemaining >= 24)
                {
                    isRefunded = true;
                    refundNotice = "Đủ điều kiện hoàn 100% tiền đặt cọc (theo chính sách hủy trước 24 giờ.";

                    var depositPayment = appt.Payments.FirstOrDefault(p => p.PaymentType == "Deposit" && p.PaymentStatus == "Success");
                    if (depositPayment != null)
                    {
                        var refundPayment = new Payment
                        {
                            AppointmentId = appt.Id,
                            Amount = depositPayment.Amount,
                            PaymentMethod = depositPayment.PaymentMethod,
                            PaymentStatus = "Success",
                            PaymentType = "Refund",
                            TransactionId = $"REF-{appt.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                            CreatedAt = DateTime.UtcNow
                        };
                        context.Payments.Add(refundPayment);
                    }
                }
                else
                {
                    isRefunded = false;
                    refundNotice = "Hủy lịch hẹn dưới 24 giờ trước giờ khám: Không được hoàn trả tiền cọc theo quy định.";
                }

                appt.Status = "Cancelled";
                appt.CancellationReason = string.IsNullOrWhiteSpace(cancellationReason) ? "Bệnh nhân yêu cầu hủy lịch" : cancellationReason.Trim();
                appt.UpdatedAt = DateTime.UtcNow;


                var slot = await context.TimeSlots.FirstOrDefaultAsync(ts => ts.Id == appt.TimeSlotId);
                if (slot != null && slot.Status != "Open")
                {
                    slot.Status = "Open";
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, $"Hủy lịch hẹn thành công. {refundNotice}", isRefunded);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Lỗi hệ thống khi hủy lịch: {ex.Message}", false);
            }
        }

        public async Task<(bool Success, string Message)> RescheduleAppointmentAsync(int appointmentId, int patientId, int newSlotId)
        {
            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var appt = await context.Appointments.Include(a => a.TimeSlot).ThenInclude(ts => ts.DoctorSchedule)
                       .FirstOrDefaultAsync(a => a.Id == appointmentId && a.PatientId == patientId);
                if (appt == null)
                {
                    return (false, "Cuộc hẹn không tồn tại.");
                }

                if (appt.Status != "Confirmed" && appt.Status != "Pending")
                {
                    return (false, $"Chỉ có thể đổi lịch cho lịch hẹn ở trạng thái 'Đã xác nhận' hoặc 'Chờ xác nhận' (Trạng thái hiện tại: {appt.Status}).");
                }

                if (appt.RescheduledCount >= 1)
                {
                    return (false, "Lịch hẹn này đã được đổi lịch 1 lần. Theo quy định, mỗi lịch hẹn chỉ được đổi tối đa 1 lần.");
                }

                var currentApptDateTime = appt.AppointmentDate.Date.Add(appt.TimeSlot.StartTime);
                if (currentApptDateTime <= DateTime.Now)
                {
                    return (false, "Không thể đổi lịch hẹn cho khung giờ đã qua.");
                }

                if (appt.TimeSlotId == newSlotId)
                {
                    return (false, "Khung giờ mới trùng với khung giờ hiện tại của bạn.");
                }

                var newSlot = await context.TimeSlots.Include(ts => ts.DoctorSchedule).FirstOrDefaultAsync(ts => ts.Id == newSlotId);

                if (newSlot == null)
                {
                    return (false, "Khung giờ mới không tồn tại.");
                }

                if (newSlot.Status != "Open")
                {
                    return (false, "Khung giờ mới đã có người khác chọn hoặc không còn khả dụng.");
                }

                if (newSlot.DoctorSchedule.DoctorId != appt.DoctorId)
                {
                    return (false, "Chỉ được phép đổi lịch sang khung giờ khác của cùng bác sĩ điều trị.");
                }

                var newSlotDateTime = newSlot.DoctorSchedule.WorkDate.Date.Add(newSlot.StartTime);
                if (newSlotDateTime <= DateTime.Now)
                {
                    return (false, "Không thể chọn khung giờ trong quá khứ.");
                }

                var oldSlot = await context.TimeSlots.FirstOrDefaultAsync(ts => ts.Id == appt.TimeSlotId);
                if(oldSlot!=null){
                oldSlot.Status = "Open";
                }

                newSlot.Status = "Booked";

                appt.TimeSlotId = newSlotId;
                appt.AppointmentDate = newSlot.DoctorSchedule.WorkDate;
                appt.RescheduledCount += 1;
                appt.UpdatedAt = DateTime.UtcNow;

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "Đổi lịch khám thành công.");
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                return (false, "Khung giờ vừa chọn đã có người khác đặt trước. Vui lòng chọn khung giờ khác.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Lỗi hệ thống khi đổi lịch: {ex.Message}");
            }
        }

    }
}
