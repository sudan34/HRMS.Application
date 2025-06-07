using HRMS.Application.Models;
using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.ViewModel
{
    public class EditAttendanceViewModel
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string EmployeeId { get; set; }
        public string? EmployeeName { get; set; } // For display only

        [Required]
        [Display(Name = "Check In")]
        public DateTime CheckIn { get; set; }
        [Display(Name = "Check Out")]
        [CustomValidation(typeof(EditAttendanceViewModel), "ValidateCheckOut")]
        public DateTime? CheckOut { get; set; }

        [Required]
        public AttendanceStatus Status { get; set; }
        
        [Required(ErrorMessage = "Remarks are required when modifying attendance")]
        [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters")]
        public string Remarks { get; set; }

        public static ValidationResult ValidateCheckOut(DateTime? checkOut, ValidationContext context)
        {
            var instance = (EditAttendanceViewModel)context.ObjectInstance;

            if (checkOut.HasValue && checkOut.Value < instance.CheckIn)
            {
                return new ValidationResult("Check Out time cannot be earlier than Check In time");
            }

            return ValidationResult.Success;
        }
    }
}
