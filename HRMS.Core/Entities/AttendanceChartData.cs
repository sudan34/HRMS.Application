namespace HRMS.Application.Models
{
    public class AttendanceChartData
    {
        public DateTime Date { get; set; }
        public int Present { get; set; }
        public int Late { get; set; }
        public int Absent { get; set; }
    }
}
