using AutoMapper;
using HRMS.Application.Data;
using HRMS.Application.Models;
using HRMS.Application.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.ViewComponents
{
    public class AttendanceReportViewComponent : ViewComponent
    {
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        public AttendanceReportViewComponent(
        IAttendanceRepository attendanceRepo,
        ApplicationDbContext context,
        IMapper mapper)
        {
            _attendanceRepo = attendanceRepo;
            _context = context;
            _mapper = mapper;
        }
        public async Task<IViewComponentResult> InvokeAsync(DateTime fromDate, DateTime toDate)
        {
            var reportData = await _attendanceRepo.GetAttendanceSummaryAsync(fromDate, toDate);

            // Map from DTO to ViewModel
            var viewModel = _mapper.Map<List<AttendanceReportViewModel>>(reportData);

            var activeEmployeesCount = await _context.Employees.CountAsync(e => e.IsActive);

            ViewData["FromDate"] = fromDate;
            ViewData["ToDate"] = toDate;
            ViewData["TotalEmployees"] = activeEmployeesCount;

            return View(viewModel);
        }
    }
}