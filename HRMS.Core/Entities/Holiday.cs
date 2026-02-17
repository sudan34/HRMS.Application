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
        public DateTime Date { get; set; }

        public string Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; }
        public DateTime UpdatedOn { get; set; }
    }
          
}