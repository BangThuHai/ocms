using BusinessObjects.Entities;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly OcmsDbContext _context;

        public AppointmentRepository(OcmsDbContext context)
        {
            _context = context;
        }

        public async Task<Appointment?> GetByIdAsync(int id)
        {
            return await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Doctor).ThenInclude(d => d.Department)
                .Include(a => a.TimeSlot).ThenInclude(t => t.DoctorSchedule).ThenInclude(s => s.Room)
                .Include(a => a.VitalSigns)
                .Include(a => a.Payments)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Appointment?> FindByQrCodeOrIdAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return null;
            }

            query = query.Trim();

            // 1. Try matching QrCodeToken directly
            var appointment = await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Doctor).ThenInclude(d => d.Department)
                .Include(a => a.TimeSlot).ThenInclude(t => t.DoctorSchedule).ThenInclude(s => s.Room)
                .Include(a => a.VitalSigns)
                .Include(a => a.Payments)
                .FirstOrDefaultAsync(a => a.QrCodeToken != null && a.QrCodeToken.ToLower() == query.ToLower());

            if (appointment != null)
            {
                return appointment;
            }

            // 2. Parse ID prefixes: OCMS-APT-X, APP-X, HAMS-APT-X
            int aptId = 0;
            if (query.StartsWith("OCMS-APT-", StringComparison.OrdinalIgnoreCase))
            {
                int.TryParse(query.Replace("OCMS-APT-", "", StringComparison.OrdinalIgnoreCase), out aptId);
            }
            else if (query.StartsWith("APP-", StringComparison.OrdinalIgnoreCase))
            {
                int.TryParse(query.Replace("APP-", "", StringComparison.OrdinalIgnoreCase), out aptId);
            }
            else if (query.StartsWith("HAMS-APT-", StringComparison.OrdinalIgnoreCase))
            {
                int.TryParse(query.Replace("HAMS-APT-", "", StringComparison.OrdinalIgnoreCase), out aptId);
            }
            else
            {
                int.TryParse(query, out aptId);
            }

            if (aptId > 0)
            {
                return await GetByIdAsync(aptId);
            }

            return null;
        }

        public async Task<Appointment?> FindTodayAppointmentByPhoneAsync(string phone, DateTime date)
        {
            phone = phone.Trim();
            var targetDate = date.Date;

            // Prioritize today's appointments that are Confirmed/Pending, then Arrived
            return await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Doctor).ThenInclude(d => d.Department)
                .Include(a => a.TimeSlot).ThenInclude(t => t.DoctorSchedule).ThenInclude(s => s.Room)
                .Include(a => a.VitalSigns)
                .Include(a => a.Payments)
                .Where(a => a.Patient.User.PhoneNumber == phone && a.AppointmentDate.Date == targetDate)
                .OrderByDescending(a => a.Status == "Confirmed")
                .ThenByDescending(a => a.Status == "Pending")
                .ThenByDescending(a => a.Status == "Arrived")
                .ThenBy(a => a.AppointmentDate)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Appointment>> GetTodayAppointmentsAsync(DateTime date)
        {
            var targetDate = date.Date;
            return await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Doctor).ThenInclude(d => d.Department)
                .Include(a => a.TimeSlot).ThenInclude(t => t.DoctorSchedule).ThenInclude(s => s.Room)
                .Include(a => a.VitalSigns)
                .Where(a => a.AppointmentDate.Date == targetDate)
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.Id)
                .ToListAsync();
        }

        public async Task<int> GetNextQueueNumberAsync(int doctorId, DateTime date)
        {
            var targetDate = date.Date;
            var maxQueue = await _context.Appointments
                .Where(a => a.DoctorId == doctorId && a.AppointmentDate.Date == targetDate && a.QueueNumber.HasValue)
                .MaxAsync(a => (int?)a.QueueNumber) ?? 0;

            return maxQueue + 1;
        }

        public async Task<Appointment> AddAppointmentAsync(Appointment appointment)
        {
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
            return appointment;
        }

        public async Task UpdateAppointmentAsync(Appointment appointment)
        {
            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();
        }

        public async Task SaveVitalSignsAsync(VitalSigns vitalSigns)
        {
            if (vitalSigns.Id == 0)
            {
                _context.VitalSigns.Add(vitalSigns);
            }
            else
            {
                _context.VitalSigns.Update(vitalSigns);
            }
            await _context.SaveChangesAsync();
        }

        public async Task<List<Doctor>> GetActiveDoctorsWithScheduleTodayAsync(DateTime date)
        {
            var targetDate = date.Date;
            return await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Department)
                .Include(d => d.Schedules.Where(s => s.WorkDate.Date == targetDate && s.Status == "Active"))
                    .ThenInclude(s => s.Room)
                .Include(d => d.Schedules.Where(s => s.WorkDate.Date == targetDate && s.Status == "Active"))
                    .ThenInclude(s => s.TimeSlots)
                .Where(d => d.Schedules.Any(s => s.WorkDate.Date == targetDate && s.Status == "Active"))
                .ToListAsync();
        }

        public async Task<List<TimeSlot>> GetOpenSlotsForDoctorTodayAsync(int doctorId, DateTime date)
        {
            var targetDate = date.Date;
            return await _context.TimeSlots
                .Include(t => t.DoctorSchedule).ThenInclude(s => s.Room)
                .Where(t => t.DoctorSchedule.DoctorId == doctorId 
                         && t.DoctorSchedule.WorkDate.Date == targetDate
                         && t.Status == "Open")
                .OrderBy(t => t.StartTime)
                .ToListAsync();
        }

        public async Task<TimeSlot?> GetTimeSlotByIdAsync(int timeSlotId)
        {
            return await _context.TimeSlots
                .Include(t => t.DoctorSchedule).ThenInclude(s => s.Room)
                .Include(t => t.DoctorSchedule).ThenInclude(s => s.Doctor).ThenInclude(d => d.User)
                .FirstOrDefaultAsync(t => t.Id == timeSlotId);
        }

        public async Task<DoctorSchedule?> GetDoctorScheduleTodayAsync(int doctorId, DateTime date)
        {
            var targetDate = date.Date;
            return await _context.DoctorSchedules
                .Include(s => s.Room)
                .Include(s => s.Doctor).ThenInclude(d => d.User)
                .Include(s => s.TimeSlots)
                .FirstOrDefaultAsync(s => s.DoctorId == doctorId && s.WorkDate.Date == targetDate && s.Status == "Active");
        }

        public async Task<TimeSlot> CreateEmergencySlotAsync(int doctorScheduleId, TimeSpan startTime, TimeSpan endTime)
        {
            var slot = new TimeSlot
            {
                ScheduleId = doctorScheduleId,
                StartTime = startTime,
                EndTime = endTime,
                Status = "Booked"
            };
            _context.TimeSlots.Add(slot);
            await _context.SaveChangesAsync();
            return slot;
        }

        public async Task<List<Appointment>> GetQueueBoardAppointmentsAsync(DateTime date)
        {
            var targetDate = date.Date;
            return await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Doctor).ThenInclude(d => d.Department)
                .Include(a => a.TimeSlot).ThenInclude(t => t.DoctorSchedule).ThenInclude(s => s.Room)
                .Where(a => a.AppointmentDate.Date == targetDate 
                         && (a.Status == "Arrived" || a.Status == "InConsultation" || a.Status == "Completed"))
                .OrderBy(a => a.QueueNumber ?? 999)
                .ThenBy(a => a.AppointmentDate)
                .ToListAsync();
        }

        public async Task<Patient?> GetPatientByPhoneAsync(string phone)
        {
            phone = phone.Trim();
            return await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.User.PhoneNumber == phone);
        }

        public async Task<Patient> CreateWalkInPatientAsync(string fullName, string phone, string gender, DateTime dob, string? address)
        {
            phone = phone.Trim();
            var existingUser = await _context.Users.Include(u => u.Patient).FirstOrDefaultAsync(u => u.PhoneNumber == phone);

            if (existingUser != null)
            {
                if (existingUser.Patient != null)
                {
                    return existingUser.Patient;
                }

                var newPatientProfile = new Patient
                {
                    UserId = existingUser.Id,
                    DateOfBirth = dob,
                    Gender = string.IsNullOrWhiteSpace(gender) ? "Other" : gender,
                    Address = address
                };
                _context.Patients.Add(newPatientProfile);
                await _context.SaveChangesAsync();
                newPatientProfile.User = existingUser;
                return newPatientProfile;
            }

            // Create new User and Patient profile
            string email = $"walkin_{phone}@ocms.local";
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                email = $"walkin_{phone}_{Guid.NewGuid().ToString().Substring(0, 5)}@ocms.local";
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword("123456");

            var newUser = new User
            {
                FullName = fullName.Trim(),
                PhoneNumber = phone,
                Email = email,
                PasswordHash = passwordHash,
                Role = "Patient",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            var newPatient = new Patient
            {
                UserId = newUser.Id,
                DateOfBirth = dob,
                Gender = string.IsNullOrWhiteSpace(gender) ? "Other" : gender,
                Address = address
            };

            _context.Patients.Add(newPatient);
            await _context.SaveChangesAsync();

            newPatient.User = newUser;
            return newPatient;
        }
    }
}
