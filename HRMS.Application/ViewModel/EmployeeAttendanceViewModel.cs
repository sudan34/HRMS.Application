using HRMS.Application.DTOs;
using HRMS.Application.Models;

namespace HRMS.Application.ViewModel
{
    public class EmployeeAttendanceViewModel
    {
        public Employee Employee { get; set; }
        public List<DailyAttendanceDto> DailyAttendance { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }
}
