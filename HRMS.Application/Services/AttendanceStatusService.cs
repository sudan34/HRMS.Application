using HRMS.Application.Data;
using HRMS.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Services
{
    public class AttendanceStatusService : IAttendanceStatusService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHolidayService _holidayService;

        public AttendanceStatusService(
            ApplicationDbContext context,
            IHolidayService holidayService)
        {
            _context = context;
            _holidayService = holidayService;
        }

        public async Task<AttendanceStatus> DetermineStatusAsync(Employee employee, DateTime checkInTime)
        {
            // Check non-working days first
            var nonWorkingStatus = await CheckNonWorkingDays(employee, checkInTime);
            if (nonWorkingStatus.HasValue)
            {
                return nonWorkingStatus.Value;
            }

            // Get department rules
            var workingHours = await GetWorkingHours(employee.DepartmentId, checkInTime);

            // Check late arrival
            return checkInTime.TimeOfDay > workingHours.LateThreshold
                ? AttendanceStatus.Late
                : AttendanceStatus.Present;
        }

        public async Task<AttendanceStatus> FinalizeStatusAsync(Attendance attendance)
        {
            // Keep existing status if not Present/Late
            if (attendance.Status != AttendanceStatus.Present &&
                attendance.Status != AttendanceStatus.Late)
            {
                return attendance.Status;
            }

            // If Employee is null, use default working hours (8 hours)
            if (attendance.Employee == null)
            {
                if (attendance.CheckOut.HasValue)
                {
                    var workDuration = attendance.CheckOut.Value - attendance.CheckIn;
                    if (workDuration.TotalHours < 7) // Default required hours
                    {
                        return AttendanceStatus.HalfDay;
                    }
                }
                return attendance.Status;
            }

            // Get working hours for the attendance date
            var workingHours = await GetWorkingHours(attendance.Employee.DepartmentId, attendance.CheckIn);

            // Check for incomplete work day
            if (attendance.CheckOut.HasValue)
            {
                var workDuration = attendance.CheckOut.Value - attendance.CheckIn;
                if (workDuration.TotalHours < workingHours.RequiredWorkHours)
                {
                    return AttendanceStatus.HalfDay;
                }
            }

            return attendance.Status;
        }

        private async Task<AttendanceStatus?> CheckNonWorkingDays(Employee employee, DateTime date)
        {
            // Check weekends
            if (await IsWeekendForEmployee(employee, date))
            {
                return AttendanceStatus.Weekend;
            }

            //// Check holidays
            //if (await _holidayService.IsHoliday(date.Date))
            //{
            //    return AttendanceStatus.Holiday;
            //}

            //// Check leave status
            //if (await IsEmployeeOnLeave(employee, date.Date))
            //{
            //    return AttendanceStatus.OnLeave;
            //}

            return null;
        }

        private async Task<WorkingHoursInfo> GetWorkingHours(int departmentId, DateTime date)
        {
            var department = await _context.Departments
                .Include(d => d.WorkingHours)
                .FirstOrDefaultAsync(d => d.Id == departmentId);

            var isFriday = date.DayOfWeek == DayOfWeek.Friday;

            // If department or working hours are missing, use defaults
            if (department?.WorkingHours == null)
            {
                return new WorkingHoursInfo
                {
                    StartTime = isFriday ? new TimeSpan(9, 30, 0) : new TimeSpan(9, 0, 0),
                    EndTime = isFriday ? new TimeSpan(13, 30, 0) : new TimeSpan(17, 0, 0),
                    LateThreshold = isFriday ? new TimeSpan(9, 45, 0) : new TimeSpan(9, 30, 0),
                    RequiredWorkHours = isFriday ? 4 : 8
                };
            }

            var workingHours = department?.WorkingHours;

            return new WorkingHoursInfo
            {
                StartTime = isFriday
                    ? workingHours?.FridayStartTime ?? new TimeSpan(9, 30, 0)
                    : workingHours?.StartTime ?? new TimeSpan(9, 0, 0),

                EndTime = isFriday
                    ? workingHours?.FridayEndTime ?? new TimeSpan(13, 30, 0)
                    : workingHours?.EndTime ?? new TimeSpan(17, 0, 0),

                LateThreshold = isFriday
                    ? workingHours?.FridayLateThreshold ?? new TimeSpan(9, 45, 0)
                    : workingHours?.LateThreshold ?? new TimeSpan(9, 30, 0),

                RequiredWorkHours = isFriday ? 4 : 8
            };
        }

        private async Task<bool> IsWeekendForEmployee(Employee employee, DateTime date)
        {
            var weekend = employee.Department?.DepartmentWeekend;
            if (weekend == null)
            {
                return date.DayOfWeek == DayOfWeek.Saturday;
            }

            return date.DayOfWeek == weekend.WeekendDay1 ||
                  (weekend.WeekendDay2.HasValue && date.DayOfWeek == weekend.WeekendDay2.Value);
        }

        //private async Task<bool> IsEmployeeOnLeave(Employee employee, DateTime date)
        //{
        //    return await _context.Leaves.AnyAsync(l =>
        //        l.EmployeeId == employee.Id &&
        //        l.StartDate <= date &&
        //        l.EndDate >= date &&
        //        l.Status == LeaveStatus.Approved);
        //}

        private record WorkingHoursInfo
        {
            public TimeSpan StartTime { get; init; }
            public TimeSpan EndTime { get; init; }
            public TimeSpan LateThreshold { get; init; }
            public double RequiredWorkHours { get; init; }
        }
    }
}

