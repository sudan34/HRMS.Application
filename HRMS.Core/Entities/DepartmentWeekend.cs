using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Application.Models
{
    public class DepartmentWeekend
    {
        [Key]
        public int Id { get; set; }
        public int DepartmentId { get; set; }
        [ForeignKey("DepartmentId")]
        public Department Department { get; set; }

        public DayOfWeek WeekendDay1 { get; set; }
        public DayOfWeek? WeekendDay2 { get; set; }
        [MaxLength(50)]
        public string WeekendType { get; set; }
    }
}
