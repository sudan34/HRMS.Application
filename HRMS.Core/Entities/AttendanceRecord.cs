namespace HRMS.Application.Models
{
    public class AttendanceRecord
    {
        public string UserId { get; set; }
        public DateTime DateTime { get; set; }
        public int VerifyMode { get; set; }
        public int InOutMode { get; set; }
        public string Status { get; set; }
    }
}
