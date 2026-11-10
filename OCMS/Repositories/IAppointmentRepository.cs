using BusinessObjects.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories
{
    public interface IAppointmentRepository
    {
        Task<Appointment> CreateAppointmentAsync(int patientId, int slotId, string? reason);
        Task<Appointment?> GetAppointmentDetailsAsync(int appointmentId);
        Task<Patient?> GetPatientByUserIdAsync(int userId);
        Task<List<Appointment>> GetAppointmentsByPatientIdAsync(int patientId);
        Task<Appointment?> GetAppointmentByIdAndPatientAsync(int appointmentId, int patientId);
        Task<(bool Success, string Message, bool IsRefunded)> CancelAppointmentAsync(int appointmentId, int patientId, string cancellationReason);
        Task<(bool Success, string Message)> RescheduleAppointmentAsync(int appointmentId, int patientId, int newSlotId);
    }
}
