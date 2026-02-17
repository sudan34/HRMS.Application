using HRMS.Application.DTOs;
using HRMS.Application.Models;

namespace HRMS.Application.Repository
{
    public interface IAttendanceRepository
    {
        Task<List<AttendanceReportDto>> GetAttendanceSummaryAsync(DateTime fromDate, DateTime toDate, int batchSizeDays = 30);
        Task<List<DailyAttendanceDto>> GetEmployeeAttendanceAsync(string employeeId, DateTime fromDate, DateTime toDate);
        Task<List<DepartmentWorkingHours>> GetDepartmentWorkingHoursAsync();
    }
}
