namespace HRMS.Application.Models
{
    public class UnknownAttendanceRecord
    {
        public int Id { get; set; }
        public string EnrollmentNumber { get; set; }
        public DateTime RecordTime { get; set; }
        public int InOutMode { get; set; }
        public bool Processed { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
