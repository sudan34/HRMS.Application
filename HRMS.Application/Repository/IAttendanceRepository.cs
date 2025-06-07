using HRMS.Application.DTOs;
using HRMS.Application.Models;

namespace HRMS.Application.Repository
{
    public interface IAttendanceRepository
    {
        Task<List<AttendanceReportDto>> GetAttendanceSummaryAsync(DateTime fromDate, DateTime toDate);
        Task<List<DailyAttendanceDto>> GetEmployeeAttendanceAsync(string employeeId, DateTime fromDate, DateTime toDate);
        Task<List<DepartmentWorkingHours>> GetDepartmentWorkingHoursAsync();
    }
}
