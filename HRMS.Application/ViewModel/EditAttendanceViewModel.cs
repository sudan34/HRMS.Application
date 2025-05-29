using HRMS.Application.Models;
using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.ViewModel
{
    public class EditAttendanceViewModel
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string EmployeeId { get; set; }

        public string? EmployeeName { get; set; } // For display only

        [Required]
        public DateTime CheckIn { get; set; }

        public DateTime? CheckOut { get; set; }

        [Required]
        public AttendanceStatus Status { get; set; }
    }
}
