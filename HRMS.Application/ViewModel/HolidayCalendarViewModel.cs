using HRMS.Application.Models;
using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.ViewModel
{
    public class HolidayCalendarViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; }
        public List<CalendarDay> CalendarDays { get; set; }
        public List<Holiday> Holidays { get; set; }
        public (int Year, int Month) PreviousMonth { get; set; }
        public (int Year, int Month) NextMonth { get; set; }
        public Dictionary<int, string> WeekDays { get; set; }
    }

    public class HolidayCreateViewModel
    {
        [Required]
        public string Name { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        public string Description { get; set; }
    }
    public class CalendarDay
    {
        public int Day { get; set; }
        public bool IsToday { get; set; }
        public bool IsEmpty { get; set; }
        public bool IsWeekend { get; set; }
    }
}
