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
    public class DoctorRepository : IDoctorRepository
    {
        private readonly OcmsDbContext context;
        public DoctorRepository(OcmsDbContext context)
        {
            this.context = context;
        }

        public async Task<List<Department>> GetAllDepartmentAsync()
        {
            return await context.Departments.ToListAsync();
        }

        public async Task<List<Doctor>> GetDoctorsAsync(int? departmentId, string? searchTerm)
        {
            var query = context.Doctors.Include(d => d.User)
                          .Include(d => d.Department)
                          .Include(d => d.DefaultRoom).AsQueryable();

            if (departmentId.HasValue && departmentId.Value > 0)
            {
                query = query.Where(d => d.DepartmentId == departmentId.Value);
            }
            if(!string.IsNullOrEmpty(searchTerm)){
                var term = searchTerm.ToLower();
                query = query.Where(d => d.User.FullName.ToLower().Contains(term) || d.Department.Name.ToLower().Contains(term));
            }
            return await query.ToListAsync();
        }

        public async Task<Doctor?> GetDoctorWithDetailsAsync(int doctorId)
        {
            return await context.Doctors
                 .Include(d => d.User)
                 .Include(d => d.Department)
                 .Include(d => d.DefaultRoom)
                 .Include(d => d.Schedules.Where(s => s.Status == "Active"))
                 .ThenInclude(s => s.TimeSlots)
                 .FirstOrDefaultAsync(d => d.Id == doctorId);
        }
    }
}
