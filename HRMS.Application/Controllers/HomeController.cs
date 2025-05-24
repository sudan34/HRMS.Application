using HRMS.Application.Data;
using HRMS.Application.Models;
using HRMS.Application.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace HRMS.Application.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }
        public async Task<IActionResult> Dashboard()
        {
            var today = DateTime.Today.AddDays(-3);

            var allEmployees = await _context.Employees.ToListAsync();

            var todaysAttendance = await _context.Attendances
                .Include(a => a.Employee)
                .Where(a => a.CheckIn.Date == today)
                .ToListAsync();

            var viewModel = new DashboardViewModel
            {
                TotalEmployees = allEmployees.Count,
                TotalPresent = todaysAttendance.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late),
                TotalAbsent = allEmployees.Count - todaysAttendance.Count,
                TotalOnLeave = todaysAttendance.Count(a => a.Status == AttendanceStatus.OnLeave),
                TodayAttendances = todaysAttendance
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
