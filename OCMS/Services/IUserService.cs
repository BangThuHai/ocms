using BusinessObjects.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public interface IUserService
    {
        Task<User?> AuthenticateAsync(string email, string password);
        Task<User> RegisterPatientAsync(string fullName, string email, string phoneNumber, string password);

        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserbyEmailAsync(string email);

        Task<bool> ResetPasswordAsync(string email, string newPassword);

    }
}
