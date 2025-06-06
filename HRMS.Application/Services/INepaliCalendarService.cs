using HRMS.Application.Models;

namespace HRMS.Application.Services
{
    public interface INepaliCalendarService
    {
        (int Year, int Month, int Day) GetCurrentBSDate();
        string GetMonthName(int month);
        Dictionary<int, string> GetWeekDays();
        List<CalendarDay> GetMonthCalendar(int year, int month);
        (int Year, int Month) GetPreviousMonth(int year, int month);
        (int Year, int Month) GetNextMonth(int year, int month);
    }
}
