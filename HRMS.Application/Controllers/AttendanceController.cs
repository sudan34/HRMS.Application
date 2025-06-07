using HRMS.Application.Data;
using HRMS.Application.Models;
using HRMS.Application.Repository;
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
        private readonly IAttendanceRepository _attendanceRepo;

        public AttendanceController(ApplicationDbContext context, IZkDeviceService zkDeviceService, UserManager<ApplicationUser> userManager, IAttendanceRepository attendanceRepo)
        {
            _context = context;
            _zkDeviceService = zkDeviceService;
            _userManager = userManager;
            _attendanceRepo = attendanceRepo;
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

            // Get employee basic info
            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee not found";
                return RedirectToAction(nameof(Index));
            }

            // Get attendance details
            var dailyAttendance = await _attendanceRepo.GetEmployeeAttendanceAsync(employeeId, defaultFromDate, defaultToDate);

            ViewBag.EmployeeId = employeeId;
            ViewBag.FromDate = defaultFromDate;
            ViewBag.ToDate = defaultToDate;

            return View(new EmployeeAttendanceViewModel
            {
                Employee = employee,
                DailyAttendance = dailyAttendance,
                FromDate = defaultFromDate,
                ToDate = defaultToDate
            });
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

            // Set default date range if not provided
            var defaultFromDate = fromDate ?? DateTime.Today.AddDays(-7);
            var defaultToDate = toDate ?? DateTime.Today;

            // Get attendance summary using repository
            var attendance = await _attendanceRepo.GetEmployeeAttendanceAsync(
                enrollmentId,
                defaultFromDate,
                defaultToDate
            );

            // Calculate summary statistics
            var summary = new MyAttendanceSummary
            {
                TotalDays = (defaultToDate - defaultFromDate).Days + 1,
                PresentDays = attendance.Count(a => a.Status == "Present" || a.Status == "Late"),
                LeaveDays = attendance.Count(a => a.Status == "Leave"),
                AbsentDays = attendance.Count(a => a.Status == "Absent"),
                HolidayDays = attendance.Count(a => a.Status == "Holiday"),
                WeekendDays = attendance.Count(a => a.Status == "Weekend"),
                AverageWorkingHours = (double)(attendance
                    .Where(a => a.WorkingHours.HasValue)
                    .Average(a => a.WorkingHours) ?? 0)
            };

            ViewBag.FromDate = defaultFromDate;
            ViewBag.ToDate = defaultToDate;
            ViewBag.EmployeeName = employee.FullName;

            return View(new MyAttendanceViewModel
            {
                DailyAttendance = attendance,
                Summary = summary
            });
        }

        //[Authorize(Roles = "HR")]
        //[HttpPost]
        //public async Task<IActionResult> UpdateAttendance(int id, DateTime checkIn, DateTime? checkOut, AttendanceStatus status)
        //{
        //    var attendance = await _context.Attendances.FindAsync(id);
        //    if (attendance == null)
        //        return NotFound();

        //    attendance.CheckIn = checkIn;
        //    attendance.CheckOut = checkOut;
        //    attendance.Status = status;

        //    _context.Update(attendance);
        //    await _context.SaveChangesAsync();

        //    return RedirectToAction(nameof(Index));
        //}

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
                Status = attendance.Status,
                Remarks = attendance.Remarks
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
                var allErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["ErrorMessage"] = string.Join("<br/>", allErrors);

                // TempData["ErrorMessage"] = "Invalid data provided.";
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

            // Get current user
            var currentUser = await _userManager.GetUserAsync(User);

            attendance.CheckIn = model.CheckIn;
            attendance.CheckOut = model.CheckOut;
            attendance.Status = model.Status;
            attendance.UpdatedOn = DateTime.Now;
            attendance.UpdatedBy = currentUser?.UserName;
            attendance.Remarks = model.Remarks;

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
