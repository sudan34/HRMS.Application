using HRMS.Application.Models;

namespace HRMS.Application.Services
{
    public interface IZkDeviceService
    {
        Task<bool> TestConnectionAsync();
        Task SyncAttendanceDataAsync();
        Task<List<AttendanceRecord>> GetAttendanceDataAsync(DateTime fromDate, DateTime toDate);
    }
}
