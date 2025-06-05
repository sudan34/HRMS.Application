using HRMS.Application.Data;
using HRMS.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Services
{
    public class AttendanceStatusService : IAttendanceStatusService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AttendanceStatusService> _logger;
        private readonly IConfiguration _config;
        private readonly IHolidayService _holidayService;

        public AttendanceStatusService(
            ApplicationDbContext context,
            ILogger<AttendanceStatusService> logger,
            IConfiguration config,
            IHolidayService holidayService)
        {
            _context = context;
            _logger = logger;
            _config = config;
            _holidayService = holidayService;
        }

        public async Task<AttendanceStatus> DetermineStatusAsync(Employee employee, DateTime checkInTime)
        {
            // Check for weekends first
            if (await IsWeekendForEmployee(employee, checkInTime))
            {
                return AttendanceStatus.Weekend;
            }

            // Check for holidays
            if (await _holidayService.IsHoliday(checkInTime.Date))
            {
                return AttendanceStatus.Holiday;
            }

            // Check if employee is on leave
            if (await IsEmployeeOnLeave(employee, checkInTime.Date))
            {
                return AttendanceStatus.OnLeave;
            }

            // Check for late arrival based on department rules
            return await CheckLateArrival(employee, checkInTime);
        }

        public async Task<AttendanceStatus> FinalizeStatusAsync(Attendance attendance)
        {
            // This method can adjust status after both check-in and check-out are recorded
            // For example, mark as half-day if worked less than required hours

            // If status was already set to weekend/holiday/leave, keep it
            if (attendance.Status != AttendanceStatus.Present &&
                attendance.Status != AttendanceStatus.Late)
            {
                return attendance.Status;
            }

            // Check for early departure if check-out exists
            if (attendance.CheckOut.HasValue)
            {
                var workDuration = attendance.CheckOut.Value - attendance.CheckIn;
                var minRequiredHours = await GetMinimumRequiredHours(attendance.Employee.DepartmentId);

                if (workDuration.TotalHours < minRequiredHours)
                {
                    return AttendanceStatus.HalfDay;
                }
            }

            return attendance.Status;
        }

        private async Task<bool> IsWeekendForEmployee(Employee employee, DateTime date)
        {
            if (employee.Department?.DepartmentWeekend == null)
            {
                // Default weekend (Saturday)
                return date.DayOfWeek == DayOfWeek.Saturday;
            }

            var weekend = employee.Department.DepartmentWeekend;
            return date.DayOfWeek == weekend.WeekendDay1 ||
                   (weekend.WeekendDay2.HasValue && date.DayOfWeek == weekend.WeekendDay2.Value);
        }

        private async Task<bool> IsEmployeeOnLeave(Employee employee, DateTime date)
        {
            return await _context.Leaves.AnyAsync(l =>
                l.EmployeeId == employee.Id &&
                l.StartDate <= date &&
                l.EndDate >= date &&
                l.Status == LeaveStatus.Approved);
        }

        private async Task<AttendanceStatus> CheckLateArrival(Employee employee, DateTime checkInTime)
        {
            var lateThreshold = await GetDepartmentLateThreshold(employee.DepartmentId);
            return checkInTime.TimeOfDay > lateThreshold
                ? AttendanceStatus.Late
                : AttendanceStatus.Present;
        }

        private async Task<TimeSpan> GetDepartmentLateThreshold(int departmentId)
        {
            // Try to get from database first
            var department = await _context.Departments
                .Include(d => d.WorkingHours)
                .FirstOrDefaultAsync(d => d.Id == departmentId);

            if (department?.WorkingHours != null)
            {
                return department.WorkingHours.LateThreshold;
            }

            // Fallback to config
            var configKey = $"DepartmentSettings:LateTimes:{departmentId}";
            var defaultTime = _config["DepartmentSettings:LateTimes:Default"] ?? "09:30:00";

            return TimeSpan.Parse(_config[configKey] ?? defaultTime);
        }

        private async Task<double> GetMinimumRequiredHours(int departmentId)
        {
            // Could be configured per department
            return 4; // Default minimum half-day hours
        }
    }
}
