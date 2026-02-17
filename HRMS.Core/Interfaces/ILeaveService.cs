using HRMS.Application.Models;

namespace HRMS.Application.Services
{
    public interface ILeaveService
    {
        Task<Leave> GetLeaveByIdAsync(int id);
        Task<List<Leave>> GetLeavesByEmployeeIdAsync(int employeeId);
        Task<List<Leave>> GetLeavesByStatusAsync(LeaveStatus status);
        Task<List<Leave>> GetLeavesInDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<Leave> CreateLeaveAsync(Leave leave);
        Task<Leave> UpdateLeaveAsync(Leave leave);
        Task<bool> DeleteLeaveAsync(int id);
        Task<bool> ApproveLeaveAsync(int id, string approvedBy);
        Task<bool> RejectLeaveAsync(int id, string rejectedBy, string reason);
        Task<bool> CancelLeaveAsync(int id, string cancelledBy);
        Task<bool> IsEmployeeOnLeaveAsync(int employeeId, DateTime date);
        Task<Leave> GetEmployeeLeaveAsync(int employeeId, DateTime date);
    }
}
