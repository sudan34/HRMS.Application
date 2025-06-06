using HRMS.Application.Services;
using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.Models
{
    public class Holiday
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public int BSYear { get; set; }  // Bikram Sambat year (e.g., 2080)

        [Required]
        public int BSMonth { get; set; } // 1-12

        [Required]
        public int BSDay { get; set; }   // 1-32

        public string Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; }
        public DateTime UpdatedOn { get; set; }
    }
          
}