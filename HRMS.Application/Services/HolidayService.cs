using HRMS.Application.Data;
using HRMS.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Services
{
    public class HolidayService : IHolidayService
    {
        private readonly ApplicationDbContext _context;

        public HolidayService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Holiday>> GetHolidaysForMonth(int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            return await GetHolidaysForDateRange(startDate, endDate);
        }

        public async Task<List<Holiday>> GetHolidaysForDateRange(DateTime startDate, DateTime endDate)
        {
            return await _context.Holidays
                .Where(h => h.Date >= startDate && h.Date <= endDate && h.IsActive)
                .OrderBy(h => h.Date)
                .ToListAsync();
        }

        public async Task AddHoliday(string name, DateTime date, string description, string createdBy)
        {
            // Check if holiday already exists for this date
            if (await _context.Holidays
                .AnyAsync(h => h.Date.Date == date.Date && h.IsActive))
            {
                throw new InvalidOperationException($"A holiday already exists for {date.ToShortDateString()}");
            }

            var holiday = new Holiday
            {
                Name = name,
                Date = date.Date, // Store only the date part
                Description = description,
                CreatedBy = createdBy,
                UpdatedOn = DateTime.Now
            };

            _context.Holidays.Add(holiday);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteHoliday(int holidayId)
        {
            var holiday = await _context.Holidays.FindAsync(holidayId);

            if (holiday == null)
            {
                throw new KeyNotFoundException($"Holiday with ID {holidayId} not found");
            }

            // Soft delete by marking as inactive
            holiday.IsActive = false;
            holiday.UpdatedOn = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsHoliday(DateTime date)
        {
            return await _context.Holidays
                .AnyAsync(h => h.Date.Date == date.Date && h.IsActive);
        }

        // Optional: Get all active holidays
        public async Task<List<Holiday>> GetAllActiveHolidays()
        {
            return await _context.Holidays
                .Where(h => h.IsActive)
                .OrderBy(h => h.Date)
                .ToListAsync();
        }
    }
}
