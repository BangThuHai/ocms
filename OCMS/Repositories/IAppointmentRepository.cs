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
    }
}
