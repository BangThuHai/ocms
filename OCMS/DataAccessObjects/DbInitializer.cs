using BCrypt.Net;
using BusinessObjects.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;


namespace DataAccessObjects
{
    public static class DbInitializer
    {
        public static void Initialize(OcmsDbContext context)
        {
            AdoptEnsureCreatedDatabase(context);
            context.Database.Migrate();
            if (context.Users.Any()) return;
            // 1. Seed Khoa (Departments)
            var deptNoi = new Department { Code = "INT", Name = "Khoa Nội Tổng Quát", Description = "Khám bệnh nội khoa tổng quát", BaseConsultationFee = 150000 };
            var deptNhi = new Department { Code = "PED", Name = "Khoa Nhi", Description = "Khám và điều trị bệnh nhi khoa", BaseConsultationFee = 150000 };
            var deptTMH = new Department { Code = "ENT", Name = "Khoa Tai Mũi Họng", Description = "Khám tai mũi họng chuyên sâu", BaseConsultationFee = 200000 };
            context.Departments.AddRange(deptNoi, deptNhi, deptTMH);
            context.SaveChanges();
            // 2. Seed Phòng khám (Rooms)
            var room101 = new Room { RoomNumber = "101", DepartmentId = deptNoi.Id, RoomType = "Consultation", IsOperational = true };
            var room102 = new Room { RoomNumber = "102", DepartmentId = deptNhi.Id, RoomType = "Consultation", IsOperational = true };
            var room201 = new Room { RoomNumber = "201", DepartmentId = deptTMH.Id, RoomType = "Consultation", IsOperational = true };
            context.Rooms.AddRange(room101, room102, room201);
            context.SaveChanges();
            // 3. Seed 4 Tài khoản test (Password mặc định: 123456)
            // (Tạm thời hash chuỗi mẫu hoặc dùng BCrypt nếu đã cài)
            string defaultHash = BCrypt.Net.BCrypt.HashPassword("123456");
            var adminUser = new User { FullName = "System Administrator", Email = "admin@ocms.clinic", PhoneNumber = "0900000001", PasswordHash = defaultHash, Role = "Admin" };
            var staffUser = new User { FullName = "Lễ Tân Thu Ngân", Email = "staff@ocms.clinic", PhoneNumber = "0900000002", PasswordHash = defaultHash, Role = "Staff" };
            var doctorUser = new User { FullName = "BS. Nguyễn Văn An", Email = "doctor@ocms.clinic", PhoneNumber = "0900000003", PasswordHash = defaultHash, Role = "Doctor" };
            var patientUser = new User { FullName = "Trần Quang Phong", Email = "patient@ocms.clinic", PhoneNumber = "0900000004", PasswordHash = defaultHash, Role = "Patient" };
            context.Users.AddRange(adminUser, staffUser, doctorUser, patientUser);
            context.SaveChanges();
            // Seed hồ sơ Bác sĩ & Bệnh nhân liên kết
            var doctorProfile = new Doctor { UserId = doctorUser.Id, DepartmentId = deptNoi.Id, DefaultRoomId = room101.Id, AcademicTitle = "ThS.BS", PricePerSlot = 150000 };
            var patientProfile = new Patient { UserId = patientUser.Id, DateOfBirth = new DateTime(2000, 1, 1), Gender = "Male", Address = "Hà Nội", HealthInsuranceNumber = "DN4010123456789" };
            context.Doctors.Add(doctorProfile);
            context.Patients.Add(patientProfile);
            // 4. Seed Thuốc mẫu (Có sẵn tồn kho)
            context.Medicines.AddRange(
                new Medicine { Name = "Paracetamol 500mg", Unit = "Viên", Price = 2000, StockQuantity = 500, IsAvailable = true },
                new Medicine { Name = "Amoxicillin 500mg", Unit = "Viên", Price = 5000, StockQuantity = 200, IsAvailable = true },
                new Medicine { Name = "Decolgen Forte", Unit = "Vỉ", Price = 15000, StockQuantity = 100, IsAvailable = true },
                new Medicine { Name = "Berocca Effervescent", Unit = "Tuýp", Price = 85000, StockQuantity = 50, IsAvailable = true }
            );
            // 5. Seed Dịch vụ Cận lâm sàng mẫu
            context.MedicalServices.AddRange(
                new MedicalService { DepartmentId = deptNoi.Id, Name = "Xét nghiệm công thức máu toàn phần", Price = 120000, IsActive = true },
                new MedicalService { DepartmentId = deptNoi.Id, Name = "Siêu âm ổ bụng tổng quát", Price = 180000, IsActive = true },
                new MedicalService { DepartmentId = deptTMH.Id, Name = "Nội soi Tai Mũi Họng", Price = 200000, IsActive = true }
            );
            context.SaveChanges();
        }

        private static void AdoptEnsureCreatedDatabase(OcmsDbContext context)
        {
            if (!context.Database.CanConnect()) return;
            var connection = context.Database.GetDbConnection();
            var openedHere = connection.State != ConnectionState.Open;
            if (openedHere) connection.Open();

            bool hasLegacySchema;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT CASE WHEN OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL AND OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Appointments', N'U') IS NOT NULL THEN 1 ELSE 0 END";
                hasLegacySchema = Convert.ToInt32(command.ExecuteScalar()) == 1;
            }

            if (openedHere) connection.Close();
            if (!hasLegacySchema) return;

            // Older builds used EnsureCreated, which creates the initial schema without migration history.
            // Mark that exact baseline so Migrate can apply additive migrations without recreating tables.
            context.Database.ExecuteSqlRaw("""
                CREATE TABLE [dbo].[__EFMigrationsHistory] (
                    [MigrationId] nvarchar(150) NOT NULL,
                    [ProductVersion] nvarchar(32) NOT NULL,
                    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                );
                INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                VALUES (N'20261016203146_InitialCreate', N'8.0.10');
                """);
        }

        }
}
