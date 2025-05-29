using HRMS.Application.Data;
using HRMS.Application.Models;
using HRMS.Application.Services;
using HRMS.Application.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IZkDeviceService _zkDeviceService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AttendanceController(ApplicationDbContext context, IZkDeviceService zkDeviceService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _zkDeviceService = zkDeviceService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
        {
            var defaultFromDate = fromDate ?? DateTime.Today.AddDays(-7);
            var defaultToDate = toDate ?? DateTime.Today;

            var attendances = await _context.Attendances
                .Include(a => a.Employee)
                 .Where(a => a.CheckIn.Date >= defaultFromDate.Date &&
                   a.CheckIn.Date <= defaultToDate.Date &&
                   a.Employee.IsActive)
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

        public async Task<IActionResult> Summary(DateTime? fromDate, DateTime? toDate)
        {
            var defaultFromDate = fromDate ?? DateTime.Today.AddDays(-30);
            var defaultToDate = toDate ?? DateTime.Today;

            if (defaultFromDate > defaultToDate)
            {
                TempData["ErrorMessage"] = "End date must be after start date";
                return RedirectToAction(nameof(Index));
            }

            //var report = await GetAttendanceReport(defaultFromDate, defaultToDate);

            ViewBag.FromDate = defaultFromDate;
            ViewBag.ToDate = defaultToDate;

            return View();
        }
       
        [HttpGet]
        public async Task<IActionResult> EmployeeReport(string employeeId, DateTime? fromDate, DateTime? toDate)
        {
            if (string.IsNullOrEmpty(employeeId))
            {
                TempData["ErrorMessage"] = "Employee ID is required.";
                return RedirectToAction(nameof(Index));
            }

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
                 .Where(e => e.IsActive)
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
            ViewBag.EmployeeId = employeeId;
            ViewBag.FromDate = defaultFromDate;
            ViewBag.ToDate = defaultToDate;

            return View(employee);
        }

        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> MyAttendance(DateTime? fromDate, DateTime? toDate)
        {
            // Get the current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            // Get the employee with the device enrollment ID
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == user.EmployeeId);
            if (employee == null)
            {
                return Unauthorized();
            }

            var enrollmentId = employee.EmployeeId;

            var defaultFromDate = fromDate ?? DateTime.Today.AddDays(-7);
            var defaultToDate = toDate ?? DateTime.Today;

            var attendances = await _context.Attendances
                .Include(a => a.Employee)
                .Where(a => a.EmployeeId == enrollmentId &&
                   a.CheckIn.Date >= defaultFromDate.Date &&
                   a.CheckIn.Date <= defaultToDate.Date &&
                   a.Employee.IsActive)
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
                return NotFound();

            attendance.CheckIn = checkIn;
            attendance.CheckOut = checkOut;
            attendance.Status = status;

            _context.Update(attendance);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendanceForEdit(int id)
        {
            var attendance = await _context.Attendances
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attendance == null)
            {
                return NotFound();
            }

            var model = new EditAttendanceViewModel
            {
                Id = attendance.Id,
                EmployeeId = attendance.EmployeeId,
                EmployeeName = $"{attendance.Employee.FirstName} {attendance.Employee.LastName}",
                CheckIn = attendance.CheckIn,
                CheckOut = attendance.CheckOut,
                Status = attendance.Status
            };

            return PartialView("_EditAttendance", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditAttendanceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView("_EditAttendance", model);
                }

                TempData["ErrorMessage"] = "Invalid data provided.";
                return RedirectToAction("Index");
            }

            var attendance = await _context.Attendances
                 .Include(a => a.Employee)
                 .FirstOrDefaultAsync(a => a.Id == model.Id);

            if (attendance == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return NotFound();
                }

                TempData["ErrorMessage"] = "Attendance record not found.";
                return RedirectToAction("Index");
            }

            attendance.CheckIn = model.CheckIn;
            attendance.CheckOut = model.CheckOut;
            attendance.Status = model.Status;

            try
            {
                _context.Update(attendance);
                await _context.SaveChangesAsync();

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true });
                }

                TempData["SuccessMessage"] = "Attendance record updated successfully.";
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, error = ex.Message });
                }

                TempData["ErrorMessage"] = "Error updating attendance record: " + ex.Message;
            }

            return RedirectToAction("Index", new
            {
                fromDate = ViewBag.FromDate ?? model.CheckIn.Date,
                toDate = ViewBag.ToDate ?? model.CheckIn.Date
            });
        }
    }
}
