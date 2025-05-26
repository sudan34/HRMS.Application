using HRMS.Application.Data;
using HRMS.Application.Models;
using HRMS.Application.ViewModel;
using Microsoft.AspNetCore.Diagnostics;
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
            var today = DateTime.Today; //.AddDays(-3);
            var lastWeek = today.AddDays(-6);

            var allEmployees = await _context.Employees.ToListAsync();
            var totalEmployees = allEmployees.Count;

            var todaysAttendance = await _context.Attendances
                .Include(a => a.Employee)
                .ThenInclude(e => e.Department)
                .Where(a => a.CheckIn.Date == today)
                .ToListAsync();
            
            // Last 7 days attendance data for the chart
            var attendanceData = await _context.Attendances
                .Where(a => a.CheckIn.Date >= lastWeek && a.CheckIn.Date <= today)
                .GroupBy(a => a.CheckIn.Date)
                .Select(g => new AttendanceChartData
                {
                    Date = g.Key,
                    Present = g.Count(a => a.Status == AttendanceStatus.Present),
                    Late = g.Count(a => a.Status == AttendanceStatus.Late),
                    Absent = allEmployees.Count - g.Count() // Simplified calculation
                })
                .OrderBy(d => d.Date)
                .ToListAsync();

            // Fill in missing days with zero values
            var chartDays = Enumerable.Range(0, 7)
                .Select(i => lastWeek.AddDays(i))
                .Select(date =>
                {
                    var data = attendanceData.FirstOrDefault(d => d.Date == date);
                    return new
                    {
                        Day = date.ToString("ddd"),
                        Present = data?.Present ?? 0,
                        Late = data?.Late ?? 0,
                        Absent = data?.Absent ?? totalEmployees
                    };
                })
                .ToList();

            // Get department distribution data
            var departmentData = await _context.Departments
                .Include(d => d.Employees)
                .Select(d => new {
                    Name = d.Name,
                    Count = d.Employees.Count
                })
                .ToListAsync();


            var viewModel = new DashboardViewModel
            {
                TotalEmployees = allEmployees.Count,
                TotalPresent = todaysAttendance.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late),
                TotalAbsent = allEmployees.Count - todaysAttendance.Count,
                TotalOnLeave = todaysAttendance.Count(a => a.Status == AttendanceStatus.OnLeave),
                TodayAttendances = todaysAttendance,
                DepartmentDistribution = departmentData.ToDictionary(d => d.Name, d => d.Count),
                ChartDays = chartDays.Select(d => d.Day).ToList(),
                PresentData = chartDays.Select(d => d.Present).ToList(),
                LateData = chartDays.Select(d => d.Late).ToList(),
                AbsentData = chartDays.Select(d => d.Absent).ToList()
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [Route("Home/Error")]
        public IActionResult Error()
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            ViewData["ErrorMessage"] = exceptionHandlerPathFeature?.Error.Message;
            return View();
        }

        [Route("Home/StatusCode/{code?}")]
        public IActionResult StatusCode(int? code)
        {
            if (code == 404)
            {
                return View("NotFound");
            }
            else if (code == 403)
            {
                return View("AccessDenied");
            }

            return View("Error");
        }
    }
}
