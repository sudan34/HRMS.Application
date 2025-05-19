using HRMS.Application.Data;
using HRMS.Application.Models;
using HRMS.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Controllers
{
    //[Authorize]
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IZkDeviceService _zkDeviceService;

        public AttendanceController(ApplicationDbContext context, IZkDeviceService zkDeviceService)
        {
            _context = context;
            _zkDeviceService = zkDeviceService;
        }

        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
        {
            var defaultFromDate = fromDate ?? DateTime.Today.AddDays(-7);
            var defaultToDate = toDate ?? DateTime.Today;

            var attendances = await _context.Attendances
                .Include(a => a.Employee)
                .Where(a => a.CheckIn >= defaultFromDate && a.CheckIn <= defaultToDate)
                .OrderByDescending(a => a.CheckIn)
                .ToListAsync();

            ViewBag.FromDate = defaultFromDate;
            ViewBag.ToDate = defaultToDate;

            return View(attendances);
        }

        [HttpPost]
        public async Task<IActionResult> SyncWithDevice()
        {
            try
            {
                await _zkDeviceService.SyncAttendanceDataAsync();
                TempData["SuccessMessage"] = "Attendance data synced successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error syncing attendance data: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        //public async Task<IActionResult> Reports()
        //{
        //    var reportData = await _context.Employees
        //        .Include(e => e.Attendances)
        //        .Select(e => new AttendanceReportViewModel
        //        {
        //            EmployeeName = $"{e.FirstName} {e.LastName}",
        //            EmployeeId = e.EmployeeId,
        //            PresentDays = e.Attendances.Count(a => a.Status == AttendanceStatus.Present),
        //            AbsentDays = e.Attendances.Count(a => a.Status == AttendanceStatus.Absent),
        //            LateDays = e.Attendances.Count(a => a.Status == AttendanceStatus.Late)
        //        })
        //        .ToListAsync();

        //    return View(reportData);
        //}
    }
}
