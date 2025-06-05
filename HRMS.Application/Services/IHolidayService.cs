using HRMS.Application.Models;

namespace HRMS.Application.Services
{
    public interface IHolidayService
    {
        Task<bool> IsHoliday(DateTime date);
        Task<List<Holiday>> GetHolidays(int year);
        Task<List<Holiday>> GetHolidaysBetween(DateTime startDate, DateTime endDate);
        Task AddHoliday(Holiday holiday);
        Task RemoveHoliday(int holidayId);
    }
}
