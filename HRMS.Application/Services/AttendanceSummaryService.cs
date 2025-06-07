using HRMS.Application.Data;
using HRMS.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Services
{
    public class AttendanceSummaryService : IAttendanceSummaryService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHolidayService _holidayService;
        private readonly ILeaveService _leaveService;

        public AttendanceSummaryService(ApplicationDbContext context,
                                     IHolidayService holidayService,
                                     ILeaveService leaveService)
        {
            _context = context;
            _holidayService = holidayService;
            _leaveService = leaveService;
        }

        public async Task GenerateDailySummaryAsync(DateTime date)
        {
            var isHoliday = await _holidayService.IsHoliday(date);
          //  var holidayName = isHoliday ? await _holidayService.GetHolidayName(date) : null;
            var isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;

            var activeEmployees = await _context.Employees
                .Where(e => e.IsActive)
                .ToListAsync();

            foreach (var employee in activeEmployees)
            {
                var attendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.EmployeeId == employee.EmployeeId &&
                                            a.CheckIn.Date == date.Date);

              //  var leave = await _leaveService.GetEmployeeLeave(employee.EmployeeId, date);

                var summary = new AttendanceSummary
                {
                    EmployeeId = employee.EmployeeId,
                    Date = date.Date,
                    IsHoliday = isHoliday,
                 //   HolidayName = holidayName,
                    IsWeekend = isWeekend,
                  //  LeaveType = leave?.LeaveType,
                    GeneratedOn = DateTime.Now
                };

                if (attendance != null)
                {
                    summary.Status = attendance.Status;
                    summary.WorkingHours = attendance.CheckOut.HasValue
                        ? attendance.CheckOut.Value - attendance.CheckIn
                        : null;
                }
                else
                {
                    summary.Status = DetermineAbsenceStatus(isHoliday, isWeekend);
                }

                _context.AttendanceSummaries.Add(summary);
            }

            await _context.SaveChangesAsync();
        }

        private AttendanceStatus DetermineAbsenceStatus(bool isHoliday, bool isWeekend)
        {
            if (isHoliday) return AttendanceStatus.Holiday;
            if (isWeekend) return AttendanceStatus.Weekend;
           // if (onLeave) return AttendanceStatus.OnLeave;
            return AttendanceStatus.Absent;
        }

        public async Task GenerateRangeSummaryAsync(DateTime fromDate, DateTime toDate)
        {
            for (var date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
            {
                await GenerateDailySummaryAsync(date);
            }
        }
    }
}
