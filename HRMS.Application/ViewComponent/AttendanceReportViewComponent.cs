using HRMS.Application.Data;
using HRMS.Application.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Add this for Include()
using System;
using System.Linq; // Add this for LINQ operations
using System.Threading.Tasks;

// Change the namespace from ViewComponent to your application's namespace
namespace HRMS.Application.ViewComponents // Changed from "ViewComponent"
{
    public class AttendanceReportViewComponent : Microsoft.AspNetCore.Mvc.ViewComponent // Fully qualified
    {
        private readonly ApplicationDbContext _context;

        public AttendanceReportViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(DateTime fromDate, DateTime toDate)
        {
            var reportData = _context.Employees
                .Include(e => e.Attendances)
                .Select(e => new
                {
                    e.EmployeeId,
                    e.FirstName,
                    e.LastName,
                    e.Email,
                    Attendances = e.Attendances.Where(a =>
                        a.CheckIn.Date >= fromDate.Date &&
                        a.CheckIn.Date <= toDate.Date)
                })
                .AsEnumerable() // Switch to client evaluation
                .Select(e => new AttendanceReportViewModel
                {
                    EmployeeId = e.EmployeeId,
                    FullName = $"{e.FirstName} {e.LastName}",
                    Email = e.Email,
                    TotalPresent = e.Attendances.Count(a => a.Status == AttendanceStatus.Present),
                    TotalLate = e.Attendances.Count(a => a.Status == AttendanceStatus.Late),
                    TotalAbsent = (toDate.Date - fromDate.Date).Days + 1 - e.Attendances.Count()
                })
                .OrderBy(e => e.FullName)
                .ToList(); // Use synchronous ToList() instead of ToListAsync()

            // For ViewComponents, use ViewData instead of ViewBag
            ViewData["FromDate"] = fromDate;
            ViewData["ToDate"] = toDate;

            return View(reportData);
        }
    }
}