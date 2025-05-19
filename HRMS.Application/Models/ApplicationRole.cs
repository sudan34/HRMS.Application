using Microsoft.AspNetCore.Identity;

namespace HRMS.Application.Models
{
    public class ApplicationRole : IdentityRole<int>
    {
        public string Description { get; set; }
        public virtual ICollection<ApplicationUserRole> UserRoles { get; set; }
    }
}
