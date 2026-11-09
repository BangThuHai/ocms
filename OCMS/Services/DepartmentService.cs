using BusinessObjects.Entities;

namespace Services
{
    public class DepartmentService : IDepartmentService
    {
        public async Task<IEnumerable<Department>> GetAllDepartmentsAsync()
        {
            return new List<Department>();
        }
    }
}