namespace HRMS.Application.Models
{
    public class AttendanceReportViewModel
    {
        public string EmployeeId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Department { get; set; }
        public string Designation { get; set; }
        public int TotalPresent { get; set; }
        public int TotalLate { get; set; }
        public int TotalHalfDay { get; set; }
        public int TotalAbsent { get; set; }
        public int TotalLeave { get; set; }
        public int TotalHoliday { get; set; }
        public int TotalWeekend { get; set; }
        public decimal TotalWorkingHours { get; set; }

        //public string EmployeeId { get; set; }
        //public string FullName { get; set; }
        //public string Email { get; set; }
        //public string Department { get; set; }
        //public int TotalPresent { get; set; }
        //public int TotalLate { get; set; }
        //public int TotalAbsent { get; set; }
        //public int TotalLeave { get; set; }
        public decimal AttendancePercentage =>
            TotalPresent > 0 ? (TotalPresent * 100m) / (TotalPresent + TotalLate + TotalAbsent) : 0;
    }
}
