using BusinessObjects.Entities;

namespace Services
{
    public class DoctorService : IDoctorService
    {
        public async Task<IEnumerable<Doctor>> GetFeaturedDoctorsAsync(int count)
        {
            return new List<Doctor>();
        }
    }
}