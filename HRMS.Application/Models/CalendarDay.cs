namespace HRMS.Application.Models
{
    public class CalendarDay
    {
        public int Day { get; set; }
        public int DayOfWeek { get; set; }
        public bool IsToday { get; set; }
        public bool IsEmpty { get; set; }
    }
}
