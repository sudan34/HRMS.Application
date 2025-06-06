using HRMS.Application.Data;
using HRMS.Application.Models;
using HRMS.Application.Services;
using HRMS.Application.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class HolidayController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INepaliCalendarService _nepaliCalendar;
        private readonly IHolidayService _holidayService;

        public HolidayController(
            ApplicationDbContext context,
            INepaliCalendarService nepaliCalendar,
            IHolidayService holidayService)
        {
            _context = context;
            _nepaliCalendar = nepaliCalendar;
            _holidayService = holidayService;
        }

        // GET: Holiday/Calendar
        public async Task<IActionResult> Calendar(int? year, int? month)
        {
            // Get current Nepali date if no parameters provided
            var currentBS = _nepaliCalendar.GetCurrentBSDate();
            int viewYear = year ?? currentBS.Year;
            int viewMonth = month ?? currentBS.Month;

            // Validate month range
            if (viewMonth < 1 || viewMonth > 12)
            {
                viewMonth = currentBS.Month;
            }

            // Get holidays for the selected month
            var holidays = await _holidayService.GetHolidaysForMonth(viewYear, viewMonth);

            // Prepare calendar view model
            var model = new HolidayCalendarViewModel
            {
                BSYear = viewYear,
                BSMonth = viewMonth,
                MonthName = _nepaliCalendar.GetMonthName(viewMonth),
                CalendarDays = _nepaliCalendar.GetMonthCalendar(viewYear, viewMonth),
                Holidays = holidays,
                PreviousMonth = _nepaliCalendar.GetPreviousMonth(viewYear, viewMonth),
                NextMonth = _nepaliCalendar.GetNextMonth(viewYear, viewMonth),

                WeekDays = _nepaliCalendar.GetWeekDays()
            };

            return View(model);
        }
        // POST: Holiday/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(HolidayCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _holidayService.AddHoliday(
                        model.Name,
                        model.BSYear,
                        model.BSMonth,
                        model.BSDay,
                        model.Description,
                        User.Identity.Name);

                    TempData["SuccessMessage"] = "Holiday added successfully";
                    return RedirectToAction(nameof(Calendar), new { year = model.BSYear, month = model.BSMonth });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            // If we got this far, something failed
            TempData["ErrorMessage"] = "Failed to add holiday";
            return RedirectToAction(nameof(Calendar), new { year = model.BSYear, month = model.BSMonth });
        }

        // POST: Holiday/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int returnYear, int returnMonth)
        {
            try
            {
                await _holidayService.DeleteHoliday(id);
                TempData["SuccessMessage"] = "Holiday deleted successfully";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Calendar), new { year = returnYear, month = returnMonth });
        }

        // GET: Holiday/GetHolidays (for AJAX)
        [HttpGet]
        public async Task<JsonResult> GetHolidays(int year, int month)
        {
            var holidays = await _holidayService.GetHolidaysForMonth(year, month);
            return Json(holidays.Select(h => new {
                id = h.Id,
                day = h.BSDay,
                name = h.Name,
                description = h.Description
            }));
        }
        //public async Task<IActionResult> Index()
        //{
        //    return View(await _context.Holidays
        //        .OrderBy(h => h.Date)
        //        .ToListAsync());
        //}

        //public IActionResult Create()
        //{
        //    return View();
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create(Holiday holiday)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        holiday.CreatedBy = User.Identity.Name;
        //        holiday.CreatedOn = DateTime.Now;
        //        _context.Add(holiday);
        //        await _context.SaveChangesAsync();
        //        return RedirectToAction(nameof(Index));
        //    }
        //    return View(holiday);
        //}

        //public async Task<IActionResult> Edit(int? id)
        //{
        //    if (id == null) return NotFound();

        //    var holiday = await _context.Holidays.FindAsync(id);
        //    if (holiday == null) return NotFound();

        //    return View(holiday);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(int id, Holiday holiday)
        //{
        //    if (id != holiday.Id) return NotFound();

        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            holiday.UpdatedBy = User.Identity.Name;
        //            holiday.UpdatedOn = DateTime.Now;
        //            _context.Update(holiday);
        //            await _context.SaveChangesAsync();
        //        }
        //        catch (DbUpdateConcurrencyException)
        //        {
        //            if (!HolidayExists(holiday.Id))
        //                return NotFound();
        //            throw;
        //        }
        //        return RedirectToAction(nameof(Index));
        //    }
        //    return View(holiday);
        //}

        //public async Task<IActionResult> Delete(int? id)
        //{
        //    if (id == null) return NotFound();

        //    var holiday = await _context.Holidays
        //        .FirstOrDefaultAsync(m => m.Id == id);
        //    if (holiday == null) return NotFound();

        //    return View(holiday);
        //}

        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteConfirmed(int id)
        //{
        //    var holiday = await _context.Holidays.FindAsync(id);
        //    if (holiday != null)
        //    {
        //        _context.Holidays.Remove(holiday);
        //        await _context.SaveChangesAsync();
        //    }
        //    return RedirectToAction(nameof(Index));
        //}

        //private bool HolidayExists(int id)
        //{
        //    return _context.Holidays.Any(e => e.Id == id);
        //}
    }
}
