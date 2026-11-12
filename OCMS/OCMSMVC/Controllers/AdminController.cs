using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; 
using OCMSMVC.Models;
using Services;
using System.Threading.Tasks;

namespace OCMSMVC.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAnalyticsService _analyticsService;

        public AdminController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
            {
                return RedirectToAction("Login", "Auth");
            }

            var viewModel = new AdminDashboardViewModel
            {
                TotalRevenue = await _analyticsService.GetTotalRevenueAsync(),
                TotalPatients = await _analyticsService.GetTotalPatientsAsync(),
                TotalAppointments = await _analyticsService.GetTotalAppointmentsAsync(),
                CancellationRate = await _analyticsService.GetCancellationRateAsync(),
                RevenueByMonth = await _analyticsService.GetRevenueByMonthAsync(),
                AppointmentsByDepartment = await _analyticsService.GetAppointmentsByDepartmentAsync()
            };

            return View(viewModel);
        }
    }
}