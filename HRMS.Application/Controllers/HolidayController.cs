using HRMS.Application.Data;
using HRMS.Application.Services;
using HRMS.Application.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        // GET: Holiday/EmployeeCalendar
        public async Task<IActionResult> EmployeeCalendar(int? year, int? month)
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

        //public async Task<IActionResult> GetHolidayDetails(int id)
        //{
        //    var holiday = await _holidayService.GetHolidayById(id); // Assume this method retrieves the holiday from the database
        //    var adDate = _holidayService.GetADDate(holiday);

        //    // Use adDate as needed
        //    return View(holiday);
        //}

        //public async Task<IActionResult> GetHolidaysInADDateRange(DateTime startDate, DateTime endDate)
        //{
        //    var holidays = await _holidayService.GetHolidaysForADDateRange(startDate, endDate);

        //    // Use holidays as needed
        //    return View(holidays);
        //}
    }
}
