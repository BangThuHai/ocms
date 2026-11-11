using BusinessObjects.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories
{
    public interface IAppointmentRepository
    {
        Task<Appointment?> GetByIdAsync(int id);
        Task<Appointment?> FindByQrCodeOrIdAsync(string query);
        Task<Appointment?> FindTodayAppointmentByPhoneAsync(string phone, DateTime date);
        Task<List<Appointment>> GetTodayAppointmentsAsync(DateTime date);
        Task<int> GetNextQueueNumberAsync(int doctorId, DateTime date);
        Task<Appointment> AddAppointmentAsync(Appointment appointment);
        Task UpdateAppointmentAsync(Appointment appointment);
        Task SaveVitalSignsAsync(VitalSigns vitalSigns);
        Task<List<Doctor>> GetActiveDoctorsWithScheduleTodayAsync(DateTime date);
        Task<List<TimeSlot>> GetOpenSlotsForDoctorTodayAsync(int doctorId, DateTime date);
        Task<TimeSlot?> GetTimeSlotByIdAsync(int timeSlotId);
        Task<DoctorSchedule?> GetDoctorScheduleTodayAsync(int doctorId, DateTime date);
        Task<TimeSlot> CreateEmergencySlotAsync(int doctorScheduleId, TimeSpan startTime, TimeSpan endTime);
        Task<List<Appointment>> GetQueueBoardAppointmentsAsync(DateTime date);
        Task<Patient?> GetPatientByPhoneAsync(string phone);
        Task<Patient> CreateWalkInPatientAsync(string fullName, string phone, string gender, DateTime dob, string? address);
    }
}
