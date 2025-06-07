using System.ComponentModel.DataAnnotations.Schema;
using static HRMS.Application.Services.AttendanceStatusService;

namespace HRMS.Application.Models
{
    public class DepartmentWorkingHours
    {
        public int Id { get; set; }
        public int DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public Department? Department { get; set; }

        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public TimeSpan LateThreshold { get; set; } = TimeSpan.FromHours(9).Add(TimeSpan.FromMinutes(30));
        public TimeSpan FridayStartTime { get; set; } = new TimeSpan(9, 30, 0); // 9:30 AM
        public TimeSpan FridayEndTime { get; set; } = new TimeSpan(13, 30, 0); // 1:30 PM
        public TimeSpan FridayLateThreshold { get; set; } = new TimeSpan(9, 45, 0); // 9:45 AM
        public bool IsFriday { get; private set; }

        public double RequiredWorkHours => IsFriday ? 4 : 8;
    }
}
