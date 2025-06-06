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
        public Dictionary<string, int> DepartmentDistribution { get; set; }
        public List<string> ChartDays { get; set; }
        public List<int> PresentData { get; set; }
        public List<int> LateData { get; set; }
        public List<int> AbsentData { get; set; }

       // public List<HolidayViewModel> UpcomingHolidays { get; set; }
    }
}
