using HRMS.Application.Controllers;

namespace HRMS.Application.ViewModel
{
    public class ManageRolesViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string EmployeeName { get; set; }
        public List<RoleViewModel> Roles { get; set; }
    }
}
