using BusinessObjects.Entities;
using Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<User?> AuthenticateAsync(string email, string password)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null || !user.IsActive)
            {
                return null;
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

            if (!isPasswordValid)
            {
                return null;
            }

            return user;
        }
        public async Task<User> RegisterPatientAsync(string fullName, string email, string phoneNumber, string password)
        {
            if (await _userRepository.ExistsByEmailAsync(email))
            {
                throw new InvalidOperationException("User with this email already exists.");
            }

            if(await _userRepository.ExistsByPhoneNumberAsync(phoneNumber))
            {
                throw new InvalidOperationException("User with this phone number already exists.");
            }

            var newUser = new User
            {
                FullName = fullName,
                Email = email,
                PhoneNumber = phoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = "Patient",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Patient = new Patient
                {
                    Gender = "Other",
                    DateOfBirth = DateTime.Today.AddYears(-20)
                }
            };
            return await _userRepository.AddAsync(newUser);
        }

        public async Task<User?> GetUserbyEmailAsync(string email)
        {
            return await _userRepository.GetByEmailAsync(email);
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _userRepository.GetByIdAsync(id);
        }        

        public async Task<bool> ResetPasswordAsync(string email, string newPassword){
            var user = await _userRepository.GetByEmailAsync(email);
            if(user==null)
            {
                return false;
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<bool> Test(User user)
        {
            return await _userRepository.ExistsByEmailAsync(user.Email);
        }
    }
}
