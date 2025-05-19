using Microsoft.AspNetCore.Identity;

namespace HRMS.Application.Models
{
    public class ApplicationUserRole : IdentityUserRole<int>
    {
        public virtual Employee User { get; set; }
        public virtual ApplicationRole Role { get; set; }
    }
}
