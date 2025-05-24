using HRMS.Application.Models;

namespace HRMS.Application.ViewModel
{
    public class DashboardViewModel
    {
        public int TotalEmployees { get; set; }
        public int TotalPresent { get; set; }
        public int TotalAbsent { get; set; }
        public int TotalOnLeave { get; set; }
        public List<Attendance> TodayAttendances { get; set; }
    }
}
