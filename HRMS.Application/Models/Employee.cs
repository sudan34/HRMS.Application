namespace HRMS.Application.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string EmployeeId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime JoinDate { get; set; }
        public int DepartmentId { get; set; }
        public Department Department { get; set; }
        public ICollection<Attendance> Attendances { get; set; }
    }
}
