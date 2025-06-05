using HRMS.Application.Data;
using HRMS.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Services
{
    public class HolidayService : IHolidayService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HolidayService> _logger;

        public HolidayService(
            ApplicationDbContext context,
            ILogger<HolidayService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> IsHoliday(DateTime date)
        {
            try
            {
                return await _context.Holidays
                    .AnyAsync(h => h.Date.Date == date.Date && h.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking holiday status for date {Date}", date);
                return false; // Default to not holiday if there's an error
            }
        }

        public async Task<List<Holiday>> GetHolidays(int year)
        {
            return await _context.Holidays
                .Where(h => h.Date.Year == year && h.IsActive)
                .OrderBy(h => h.Date)
                .ToListAsync();
        }

        public async Task<List<Holiday>> GetHolidaysBetween(DateTime startDate, DateTime endDate)
        {
            return await _context.Holidays
                .Where(h => h.Date >= startDate.Date && h.Date <= endDate.Date && h.IsActive)
                .OrderBy(h => h.Date)
                .ToListAsync();
        }

        public async Task AddHoliday(Holiday holiday)
        {
            // Check for existing holiday on the same date
            var exists = await _context.Holidays
                .AnyAsync(h => h.Date.Date == holiday.Date.Date && h.IsActive);

            if (exists)
            {
                throw new InvalidOperationException($"Holiday already exists for date {holiday.Date:d}");
            }

            holiday.IsActive = true;
            holiday.CreatedOn = DateTime.Now;

            _context.Holidays.Add(holiday);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveHoliday(int holidayId)
        {
            var holiday = await _context.Holidays.FindAsync(holidayId);
            if (holiday != null)
            {
                // Soft delete
                holiday.IsActive = false;
                holiday.UpdatedOn = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        // Optional: Method to load holidays from external API or file
        public async Task LoadHolidaysFromSource(int year, string countryCode)
        {
            // Implementation would depend on your holiday data source
            // Could be government API, CSV file, etc.
        }
    }
}
