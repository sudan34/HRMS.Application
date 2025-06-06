using HRMS.Application.Data;
using HRMS.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Services
{
    public class HolidayService : IHolidayService
    {
        private readonly ApplicationDbContext _context;
        private readonly INepaliDateConverter _dateConverter;

        public HolidayService(ApplicationDbContext context, INepaliDateConverter dateConverter)
        {
            _context = context;
            _dateConverter = dateConverter;
        }

        public async Task<List<Holiday>> GetHolidaysForMonth(int bsYear, int bsMonth)
        {
            return await _context.Holidays
                .Where(h => h.BSYear == bsYear && h.BSMonth == bsMonth && h.IsActive)
                .OrderBy(h => h.BSDay)
                .ToListAsync();
        }

        public async Task AddHoliday(string name, int bsYear, int bsMonth, int bsDay, string description, string createdBy)
        {
            if (!_dateConverter.IsValidBSDate(bsYear, bsMonth, bsDay))
                throw new ArgumentException("Invalid Nepali date");

            if (await _context.Holidays.AnyAsync(h =>
                h.BSYear == bsYear && h.BSMonth == bsMonth && h.BSDay == bsDay && h.IsActive))
                throw new ArgumentException("Holiday already exists for this date");

            var holiday = new Holiday
            {
                Name = name,
                BSYear = bsYear,
                BSMonth = bsMonth,
                BSDay = bsDay,
                Description = description,
                CreatedBy = createdBy,
                CreatedOn = DateTime.Now,
                IsActive = true
            };

            _context.Holidays.Add(holiday);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteHoliday(int id)
        {
            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday == null)
                throw new ArgumentException("Holiday not found");

            // Soft delete
            holiday.IsActive = false;
            holiday.UpdatedOn = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsHoliday(DateTime adDate)
        {
            var bsDate = _dateConverter.ConvertToBS(adDate);
            return await _context.Holidays
                .AnyAsync(h => h.BSYear == bsDate.Year &&
                              h.BSMonth == bsDate.Month &&
                              h.BSDay == bsDate.Day &&
                              h.IsActive);
        }
    }
}
