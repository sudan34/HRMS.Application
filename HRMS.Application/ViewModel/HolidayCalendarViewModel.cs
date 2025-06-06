using HRMS.Application.Models;
using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.ViewModel
{
    public class HolidayCalendarViewModel
    {
        public int BSYear { get; set; }
        public int BSMonth { get; set; }
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
        public int BSYear { get; set; }

        [Required]
        public int BSMonth { get; set; }

        [Required]
        public int BSDay { get; set; }

        public string Description { get; set; }
    }
}
