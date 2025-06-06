using HRMS.Application.Models;

namespace HRMS.Application.Services
{
    public interface IHolidayService
    {
        Task<List<Holiday>> GetHolidaysForMonth(int year, int month);
        Task<List<Holiday>> GetHolidaysForDateRange(DateTime startDate, DateTime endDate);
        Task AddHoliday(string name, DateTime date, string description, string createdBy);
        Task DeleteHoliday(int holidayId);
        Task<bool> IsHoliday(DateTime date);
    }
}
