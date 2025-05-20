using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Application.Models
{
    public class ApplicationUser : IdentityUser
    {
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; }

        // Add any additional properties here
        public string CustomRoleType { get; set; }
    }
}
