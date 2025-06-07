using HRMS.Application.Models;

namespace HRMS.Application.Services
{
    public interface IAttendanceStatusService
    {
        Task<AttendanceStatus> DetermineStatusAsync(Employee employee, DateTime checkInTime);
        Task<AttendanceStatus> FinalizeStatusAsync(Attendance attendance);
      //  Task<DepartmentRules> GetDepartmentRules(int departmentId);
    }
}
