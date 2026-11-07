using BusinessObjects.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories
{
    public interface IDoctorRepository
    {
        Task<List<Doctor>> GetDoctorsAsync(int? department, string? searchTerm);
        Task<Doctor?> GetDoctorWithDetailsAsync(int doctorId);
        Task<List<Department>> GetAllDepartmentAsync();
    }
}
