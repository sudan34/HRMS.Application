using HRMS.Application.Models;

namespace HRMS.Application.ViewModel
{
    public class AttendanceSummaryViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<AttendanceSummary> Summaries { get; set; }
        public SummaryStatsViewModel SummaryStats { get; set; }
    }
}