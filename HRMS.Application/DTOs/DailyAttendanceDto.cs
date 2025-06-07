namespace HRMS.Application.DTOs
{
    public class DailyAttendanceDto
    {
        public DateTime Date { get; set; }
        public string Day { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public decimal? WorkingHours { get; set; }
        public string WorkingHoursDisplay { get; set; }
        public string Status { get; set; }
        public string StatusReason { get; set; } // Holiday, Weekend, Leave, etc.
    }
}
