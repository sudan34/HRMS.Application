namespace HRMS.Application.Services
{
    public interface IAttendanceSummaryService
    {
        Task GenerateDailySummaryAsync(DateTime date);
        Task GenerateRangeSummaryAsync(DateTime fromDate, DateTime toDate);
    }
}
