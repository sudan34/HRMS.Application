using HRMS.Application.Data;
using HRMS.Application.Services;
using HRMS.Application.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Application.Controllers
{
    [Authorize]
    public class HolidayController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHolidayService _holidayService;

        public HolidayController(
            ApplicationDbContext context,
            IHolidayService holidayService)
        {
            _context = context;
            _holidayService = holidayService;
        }

        // GET: Holiday/Calendar
        [Authorize(Roles = "HR,SuperAdmin")]
        public async Task<IActionResult> Calendar(int? year, int? month)
        {
            // Get current date if no parameters provided
            var currentDate = DateTime.Now;
            int viewYear = year ?? currentDate.Year;
            int viewMonth = month ?? currentDate.Month;

            // Validate month range
            if (viewMonth < 1 || viewMonth > 12)
            {
                viewMonth = currentDate.Month;
            }

            // Get holidays for the selected month
            var startDate = new DateTime(viewYear, viewMonth, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            var holidays = await _holidayService.GetHolidaysForDateRange(startDate, endDate);

            // Prepare calendar view model
            var model = new HolidayCalendarViewModel
            {
                Year = viewYear,
                Month = viewMonth,
                MonthName = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(viewMonth),
                CalendarDays = GenerateCalendarDays(viewYear, viewMonth),
                Holidays = holidays,
                PreviousMonth = GetPreviousMonth(viewYear, viewMonth),
                NextMonth = GetNextMonth(viewYear, viewMonth),
                WeekDays = GetWeekDays()
            };

            return View(model);
        }
        // POST: Holiday/Add
        [Authorize(Roles = "HR,SuperAdmin")]
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
                        model.Date,
                        model.Description,
                        User.Identity.Name);

                    TempData["SuccessMessage"] = "Holiday added successfully";
                    return RedirectToAction(nameof(Calendar), new { year = model.Date.Year, month = model.Date.Month });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            TempData["ErrorMessage"] = "Failed to add holiday";
            return RedirectToAction(nameof(Calendar), new { year = model.Date.Year, month = model.Date.Month });
        }
        // POST: Holiday/Delete
        [Authorize(Roles = "HR,SuperAdmin")]
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
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            var holidays = await _holidayService.GetHolidaysForDateRange(startDate, endDate);

            return Json(holidays.Select(h => new {
                id = h.Id,
                day = h.Date.Day,
                name = h.Name,
                description = h.Description
            }));
        }

        // GET: Holiday/EmployeeCalendar
        public async Task<IActionResult> EmployeeCalendar(int? year, int? month)
        {
            // Get current date if no parameters provided
            var currentDate = DateTime.Now;
            int viewYear = year ?? currentDate.Year;
            int viewMonth = month ?? currentDate.Month;

            // Validate month range
            if (viewMonth < 1 || viewMonth > 12)
            {
                viewMonth = currentDate.Month;
            }

            // Get holidays for the selected month
            var startDate = new DateTime(viewYear, viewMonth, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            var holidays = await _holidayService.GetHolidaysForDateRange(startDate, endDate);

            // Prepare calendar view model
            var model = new HolidayCalendarViewModel
            {
                Year = viewYear,
                Month = viewMonth,
                MonthName = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(viewMonth),
                CalendarDays = GenerateCalendarDays(viewYear, viewMonth),
                Holidays = holidays,
                PreviousMonth = GetPreviousMonth(viewYear, viewMonth),
                NextMonth = GetNextMonth(viewYear, viewMonth),
                WeekDays = GetWeekDays()
            };

            return View(model);
        }

        #region Helper Methods
        private List<CalendarDay> GenerateCalendarDays(int year, int month)
        {
            var days = new List<CalendarDay>();
            var firstDayOfMonth = new DateTime(year, month, 1);
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var currentDate = DateTime.Now;

            // Add empty days for the first week
            int firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
            for (int i = 0; i < firstDayOfWeek; i++)
            {
                days.Add(new CalendarDay { IsEmpty = true });
            }

            // Add actual days of the month
            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(year, month, day);
                days.Add(new CalendarDay
                {
                    Day = day,
                    IsToday = date.Date == currentDate.Date,
                    IsWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday
                });
            }

            return days;
        }

        private (int Year, int Month) GetPreviousMonth(int year, int month)
        {
            if (month == 1)
                return (year - 1, 12);
            return (year, month - 1);
        }

        private (int Year, int Month) GetNextMonth(int year, int month)
        {
            if (month == 12)
                return (year + 1, 1);
            return (year, month + 1);
        }

        private Dictionary<int, string> GetWeekDays()
        {
            return new Dictionary<int, string>
            {
                { 0, "Sun" },
                { 1, "Mon" },
                { 2, "Tue" },
                { 3, "Wed" },
                { 4, "Thu" },
                { 5, "Fri" },
                { 6, "Sat" }
            };
        }
        #endregion
    }
}

