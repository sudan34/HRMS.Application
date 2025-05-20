namespace HRMS.Application.ViewModel
{
    public class UserRolesViewModel
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string EmployeeName { get; set; }
        public IEnumerable<string> Roles { get; set; }
    }
}
