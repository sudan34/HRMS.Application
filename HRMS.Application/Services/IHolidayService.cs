using HRMS.Application.Models;

namespace HRMS.Application.Services
{
    public interface IHolidayService
    {
        Task<List<Holiday>> GetHolidaysForMonth(int bsYear, int bsMonth);
        Task AddHoliday(string name, int bsYear, int bsMonth, int bsDay, string description, string createdBy);
        Task DeleteHoliday(int holidayId);

        Task<bool> IsHoliday(DateTime date);

        //Task<List<Holiday>> GetHolidays(int year);
        //Task<List<Holiday>> GetHolidaysBetween(DateTime startDate, DateTime endDate);
        //Task AddHoliday(Holiday holiday);
        //Task RemoveHoliday(int holidayId);
    }
}
