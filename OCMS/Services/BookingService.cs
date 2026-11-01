using BusinessObjects.Entities;
using Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class BookingService : IBookingService
    {
        private readonly IDoctorRepository doctorRepository;
        private readonly ITimeSlotRepository timeSlotRepository;
        private readonly ICapacityEngine capacityEngine;

        public BookingService(ICapacityEngine capacityEngine, IDoctorRepository doctorRepository, ITimeSlotRepository timeSlotRepository)
        {
            this.capacityEngine = capacityEngine;
            this.doctorRepository = doctorRepository;
            this.timeSlotRepository = timeSlotRepository;
        }
        public async Task<List<Department>> GetSpecialtiesAsync()
        {
            return await doctorRepository.GetAllDepartmentAsync();
        }
        public async Task<List<Doctor>> SearchDoctorAsync(int? departmentId, string? term)
        {
            return await doctorRepository.GetDoctorsAsync(departmentId, term);
        }
        public async Task<Doctor?> GetDoctorWithDaySlotsAsync(int doctorId, DateTime targetDate)
        {
            return await doctorRepository.GetDoctorWithDetailsAsync(doctorId);
        }
        public async Task<HoldSlotResult> HoldTimeSlotAsync(int slotId, string rowVersion, int? patientId)
        {
            byte[] rowVersionBytes;

            try
            {
                rowVersionBytes = Convert.FromBase64String(rowVersion);
            }
            catch
            {
                return new HoldSlotResult
                {
                    IsSuccess = false,
                    Message = "Mã xác thực token của khung giờ không hợp lệ.",
                };

            }

            var success = await timeSlotRepository.HoldSlotAsync(slotId, rowVersionBytes);

            if (success)
            {
                return new HoldSlotResult
                {
                    IsSuccess = true,
                    SlotId = slotId,
                    NewStatus = "Hold",
                    ExpiresInSeconds = 600,
                    Message = "Giữ chỗ thành công! Bạn có 10 phút để hoàn tất xác nhận thông tin khám."
                };
            }

            //conflict 
            var currentSlot = await timeSlotRepository.GetSlotByIdAsync(slotId);
            var alternatives = new List<AlternativeSlotDto>();

            if (currentSlot != null)
            {
                var daySlots = await timeSlotRepository.GetSlotsByDoctorAndDateAsync(currentSlot.DoctorSchedule.DoctorId, currentSlot.DoctorSchedule.WorkDate);

                var suggested = capacityEngine.GetAlternativeSlots(daySlots, slotId, 3);
                alternatives = suggested.Select(s => new AlternativeSlotDto
                {
                    Id = s.Id,
                    TimeRange = $"{s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm}",
                    RowVersion = Convert.ToBase64String(s.RowVersion)
                }).ToList();
            }

            return new HoldSlotResult
            {
                IsSuccess = false,
                SlotId = slotId,
                Message = "Rất tiếc! Khung giờ này vừa có bệnh nhân khác giữ trước. Vui lòng chọn khung giờ khác.",
                AlternativeSlots = alternatives
            };
        }
    }
}
