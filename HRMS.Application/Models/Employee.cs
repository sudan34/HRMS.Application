using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Application.Models
{
    public class Employee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        [MaxLength(50)]
        public string EmployeeId { get; set; }  // This is the enrollment ID from the device

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        [MaxLength(100)]
        public string Email { get; set; }
        public string? Designation { get; set; }

        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        public DateTime JoinDate { get; set; } = DateTime.Now;
        public DateTime? ResignDate { get; set; }
        public int DepartmentId { get; set; } = 1;
        public Department? Department { get; set; }
        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public bool IsActive { get; set; } = true;
    }
}
