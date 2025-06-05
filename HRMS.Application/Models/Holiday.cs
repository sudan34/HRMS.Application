using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Application.Models
{
    public class Holiday
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public bool IsRecurring { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? UpdatedBy { get; set; }

        // For recurring holidays
        public HolidayRecurrence? Recurrence { get; set; }
    }

    public class HolidayRecurrence
    {
        public int Id { get; set; }
        public int HolidayId { get; set; }

        [ForeignKey("HolidayId")]
        public Holiday Holiday { get; set; }

        public RecurrenceType RecurrenceType { get; set; } // Annual, Monthly, etc.
        public int? DayOfMonth { get; set; }
        public Month? Month { get; set; }
        public DayOfWeek? DayOfWeek { get; set; }
        public int? WeekOfMonth { get; set; } // First, Second, etc.
    }

    public enum RecurrenceType
    {
        Annual,
        Monthly,
        Weekly,
        Custom
    }

    public enum Month
    {
        January = 1,
        February,
        March,
        April,
        May,
        June,
        July,
        August,
        September,
        October,
        November,
        December
    }
}