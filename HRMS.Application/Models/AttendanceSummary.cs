using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Application.Models
{
    public class AttendanceSummary
    {
      //  [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string EmployeeId { get; set; }

       // [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public AttendanceStatus Status { get; set; }

        public TimeSpan? WorkingHours { get; set; }
        public bool IsHoliday { get; set; }
        public bool IsWeekend { get; set; }
        public string HolidayName { get; set; }
        public string LeaveType { get; set; }
        public string Remarks { get; set; }
        public DateTime GeneratedOn { get; set; } = DateTime.Now;
    }
}
