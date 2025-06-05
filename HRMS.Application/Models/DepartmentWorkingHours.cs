using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Application.Models
{
    public class DepartmentWorkingHours
    {
        public int Id { get; set; }
        public int DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public Department Department { get; set; }

        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public TimeSpan LateThreshold { get; set; } = TimeSpan.FromHours(9).Add(TimeSpan.FromMinutes(30));
        public double RequiredWorkHours { get; set; } = 8;
    }
}
