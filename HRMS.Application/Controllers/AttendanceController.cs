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
                // If using AJAX, return JSON for better handling
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    await _zkDeviceService.SyncAttendanceDataAsync();
                    return Json(new { success = true, message = "Sync completed successfully" });
                }

                // Regular form submission
                await _zkDeviceService.SyncAttendanceDataAsync();
                TempData["SuccessMessage"] = "Attendance data synced successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error syncing attendance data: {ex.Message}";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<List<AttendanceReportViewModel>> GetAttendanceReport(DateTime fromDate, DateTime toDate)
        {
            return await _context.Employees
                .Include(e => e.Attendances)
                .Select(e => new AttendanceReportViewModel
                {
                    EmployeeId = e.EmployeeId,
                    FullName = e.FullName,
                    Email = e.Email,
                    TotalPresent = e.Attendances.Count(a =>
                        a.CheckIn.Date >= fromDate.Date &&
                        a.CheckIn.Date <= toDate.Date &&
                        a.Status == AttendanceStatus.Present),
                    TotalLate = e.Attendances.Count(a =>
                        a.CheckIn.Date >= fromDate.Date &&
                        a.CheckIn.Date <= toDate.Date &&
                        a.Status == AttendanceStatus.Late),
                    TotalAbsent = (toDate.Date - fromDate.Date).Days + 1 -
                        e.Attendances.Count(a =>
                            a.CheckIn.Date >= fromDate.Date &&
                            a.CheckIn.Date <= toDate.Date)
                })
                .OrderBy(e => e.FullName)
                .ToListAsync();
        }

        [HttpGet]
        public async Task<IActionResult> EmployeeReport(string employeeId, DateTime? fromDate, DateTime? toDate)
        {
            // Set default date range if not provided
            var defaultFromDate = fromDate ?? DateTime.Today.AddDays(-30);
            var defaultToDate = toDate ?? DateTime.Today;

            // Validate date range
            if (defaultFromDate > defaultToDate)
            {
                TempData["ErrorMessage"] = "End date must be after start date";
                return RedirectToAction(nameof(Index));
            }

            // Get employee with attendance records for the date range
            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Attendances
                    .Where(a => a.CheckIn.Date >= defaultFromDate.Date &&
                               a.CheckIn.Date <= defaultToDate.Date))
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee not found";
                return RedirectToAction(nameof(Index));
            }

            // Prepare view data
            ViewBag.FromDate = defaultFromDate;
            ViewBag.ToDate = defaultToDate;

            return View(employee);
        }

        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> MyAttendance(DateTime? fromDate, DateTime? toDate)
        {
            var employeeId = int.Parse(User.FindFirst("EmployeeId").Value);

            var defaultFromDate = fromDate ?? DateTime.Today.AddDays(-7);
            var defaultToDate = toDate ?? DateTime.Today;

            var attendances = await _context.Attendances
                .Include(a => a.Employee)
                .Where(a => a.EmployeeId == employeeId &&
                           a.CheckIn >= defaultFromDate &&
                           a.CheckIn <= defaultToDate)
                .OrderByDescending(a => a.CheckIn)
                .ToListAsync();

            ViewBag.FromDate = defaultFromDate;
            ViewBag.ToDate = defaultToDate;

            return View(attendances);
        }

        [Authorize(Roles = "HR")]
        [HttpPost]
        public async Task<IActionResult> UpdateAttendance(int id, DateTime checkIn, DateTime? checkOut, AttendanceStatus status)
        {
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance == null)
            {
                return NotFound();
            }

            attendance.CheckIn = checkIn;
            attendance.CheckOut = checkOut;
            attendance.Status = status;

            _context.Update(attendance);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

    }
}
