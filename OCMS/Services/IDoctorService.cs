using BusinessObjects.Entities;

namespace Services
{
    public interface IDoctorService
    {
        Task<IEnumerable<Doctor>> GetFeaturedDoctorsAsync(int count);
    }
}