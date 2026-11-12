using BusinessObjects.Entities;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services
{
    public class DoctorService : IDoctorService
    {
        private readonly OcmsDbContext _dbContext;

        public DoctorService(OcmsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Doctor>> GetFeaturedDoctorsAsync(int count)
        {
            return await _dbContext.Doctors
                .Include(d => d.User)
                .Include(d => d.Department)
                .Take(count)
                .ToListAsync();
        }
    }
}