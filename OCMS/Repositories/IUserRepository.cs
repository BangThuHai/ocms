using BusinessObjects.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<bool> ExistsByEmailAsync(string email);
        Task<bool> ExistsByPhoneNumberAsync(string phoneNumber);
        Task<User> AddAsync(User user);
        Task UpdateAsync(User user);
        Task<List<User>> GetAllAsync();
    }
}
