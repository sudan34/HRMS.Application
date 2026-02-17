// LeaveService.cs
using HRMS.Application.Data;
using HRMS.Application.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HRMS.Application.Services
{
    public class LeaveService : ILeaveService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LeaveService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<Leave> GetLeaveByIdAsync(int id)
        {
            return await _context.Leaves
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task<List<Leave>> GetLeavesByEmployeeIdAsync(int employeeId)
        {
            return await _context.Leaves
                .Where(l => l.EmployeeId == employeeId)
                .OrderByDescending(l => l.StartDate)
                .ToListAsync();
        }

        public async Task<List<Leave>> GetLeavesByStatusAsync(LeaveStatus status)
        {
            return await _context.Leaves
                .Where(l => l.Status == status)
                .OrderBy(l => l.StartDate)
                .ToListAsync();
        }

        public async Task<List<Leave>> GetLeavesInDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Leaves
                .Where(l => l.StartDate <= endDate && l.EndDate >= startDate)
                .Include(l => l.Employee)
                .OrderBy(l => l.StartDate)
                .ToListAsync();
        }

        public async Task<Leave> CreateLeaveAsync(Leave leave)
        {
            leave.CreatedOn = DateTime.Now;
          //  leave.CreatedBy = await _userManager.GetUserAsync(User);
            leave.Status = LeaveStatus.Pending;

            _context.Leaves.Add(leave);
            await _context.SaveChangesAsync();
            return leave;
        }

        public async Task<Leave> UpdateLeaveAsync(Leave leave)
        {

          //  var currentUser = await _userManager.GetUserAsync(User);

            leave.UpdatedOn = DateTime.Now;
          //  leave.UpdatedBy = await _userManager.GetUserAsync(User);

            _context.Leaves.Update(leave);
            await _context.SaveChangesAsync();
            return leave;
        }

        public async Task<bool> DeleteLeaveAsync(int id)
        {
            var leave = await GetLeaveByIdAsync(id);
            if (leave == null) return false;

            _context.Leaves.Remove(leave);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ApproveLeaveAsync(int id, string approvedBy)
        {
            var leave = await GetLeaveByIdAsync(id);
            if (leave == null) return false;

            leave.Status = LeaveStatus.Approved;
            leave.UpdatedOn = DateTime.Now;
            leave.UpdatedBy = approvedBy;

            _context.Leaves.Update(leave);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectLeaveAsync(int id, string rejectedBy, string reason)
        {
            var leave = await GetLeaveByIdAsync(id);
            if (leave == null) return false;

            leave.Status = LeaveStatus.Rejected;
            leave.Reason = reason;
            leave.UpdatedOn = DateTime.Now;
            leave.UpdatedBy = rejectedBy;

            _context.Leaves.Update(leave);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelLeaveAsync(int id, string cancelledBy)
        {
            var leave = await GetLeaveByIdAsync(id);
            if (leave == null) return false;

            leave.Status = LeaveStatus.Cancelled;
            leave.UpdatedOn = DateTime.Now;
            leave.UpdatedBy = cancelledBy;

            _context.Leaves.Update(leave);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsEmployeeOnLeaveAsync(int employeeId, DateTime date)
        {
            return await _context.Leaves
                .AnyAsync(l => l.EmployeeId == employeeId &&
                              l.Status == LeaveStatus.Approved &&
                              l.StartDate <= date &&
                              l.EndDate >= date);
        }

        public async Task<Leave> GetEmployeeLeaveAsync(int employeeId, DateTime date)
        {
            return await _context.Leaves
                .FirstOrDefaultAsync(l => l.EmployeeId == employeeId &&
                                         l.Status == LeaveStatus.Approved &&
                                         l.StartDate <= date &&
                                         l.EndDate >= date);
        }
    }
}