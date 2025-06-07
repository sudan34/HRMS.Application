namespace HRMS.Application.ViewModel
{
    public class SummaryStatsViewModel
    {
        public int TotalEmployees { get; set; }
        public int TotalPresent { get; set; }
        public int TotalLate { get; set; }
        public int TotalAbsent { get; set; }
        public int TotalOnLeave { get; set; }
        public int TotalHolidays { get; set; }
        public int TotalWeekend { get; set; }
        public double AverageWorkingHours { get; set; }
    }
}