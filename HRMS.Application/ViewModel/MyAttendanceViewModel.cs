using HRMS.Application.DTOs;

namespace HRMS.Application.ViewModel
{
    public class MyAttendanceViewModel
    {
        public List<DailyAttendanceDto> DailyAttendance { get; set; }
        public MyAttendanceSummary Summary { get; set; }
    }

    public class MyAttendanceSummary
    {
        public int TotalDays { get; set; }
        public int PresentDays { get; set; }
        public int LeaveDays { get; set; }
        public int AbsentDays { get; set; }
        public int HolidayDays { get; set; }
        public int WeekendDays { get; set; }
        public double AverageWorkingHours { get; set; }
    }
}
