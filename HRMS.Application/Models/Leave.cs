using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Application.Models
{
    public class Leave
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public LeaveType Type { get; set; }

        [MaxLength(500)]
        public string Reason { get; set; }
        [Required]
        public LeaveStatus Status { get; set; } = LeaveStatus.Pending;

        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public enum LeaveType
    {
        Sick,
        Vacation,
        Personal,
        Maternity,
        Paternity,
        Bereavement,
        Other
    }
    public enum LeaveStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled
    }

}
