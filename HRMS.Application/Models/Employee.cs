using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Application.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string EmployeeId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
        public string Email { get; set; }
        public string Phone { get; set; } = string.Empty;
        public DateTime JoinDate { get; set; } = DateTime.Now;
        public int DepartmentId { get; set; } = 1;
        public Department Department { get; set; }
        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public bool IsActive { get; set; } = true;
    }
}
