using HRMS.Application.Models;

namespace HRMS.Application.Services
{
    public class NepaliCalendarService : INepaliCalendarService
    {
        // Nepali month names
        private readonly Dictionary<int, string> _monthNames = new()
        {
            {1, "बैशाख"}, {2, "जेठ"}, {3, "असार"}, {4, "साउन"}, {5, "भदौ"}, {6, "असोज"},
            {7, "कार्तिक"}, {8, "मंसिर"}, {9, "पुष"}, {10, "माघ"}, {11, "फाल्गुण"}, {12, "चैत"}
        };

        // Weekday names
        private readonly Dictionary<int, string> _weekDays = new()
        {
            {0, "आइत"}, {1, "सोम"}, {2, "मंगल"}, {3, "बुध"}, {4, "बिही"}, {5, "शुक्र"}, {6, "शनि"}
        };

        // Days in each Nepali month (for a given year)
        private readonly Dictionary<int, int[]> _nepaliCalendarData = new()
        {
            // Format: Year => [Days in each month]
            {2080, new[] {31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30}},
            {2081, new[] {31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30}},
            {2082, new[] {31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30}},
            {2083, new[] {31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30}},
            {2084, new[] {31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30}},
            {2085, new[] {31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30}},
            // Add more years as needed
        };

        public (int Year, int Month, int Day) GetCurrentBSDate()
        {
            // Implement actual AD to BS conversion here
            // For now returning a hardcoded value
            return (2080, 5, 15);
        }

        public string GetMonthName(int month) => _monthNames.GetValueOrDefault(month, "");

        public Dictionary<int, string> GetWeekDays() => _weekDays;

        public List<CalendarDay> GetMonthCalendar(int year, int month)
        {
            var daysInMonth = _nepaliCalendarData[year][month - 1];
            var calendarDays = new List<CalendarDay>();

            // Determine the first day of the month
            var firstDayOfMonth = 2; // Example: Monday (needs actual calculation)

            // Add empty cells for days before the 1st of the month
            for (int i = 0; i < firstDayOfMonth; i++)
            {
                calendarDays.Add(new CalendarDay { IsEmpty = true });
            }

            // Add actual days of the month
            for (int day = 1; day <= daysInMonth; day++)
            {
                calendarDays.Add(new CalendarDay
                {
                    Day = day,
                    DayOfWeek = (firstDayOfMonth + day - 1) % 7,
                    IsToday = (year == 2080 && month == 5 && day == 15) // Example
                });
            }

            return calendarDays;
        }

        public (int Year, int Month) GetPreviousMonth(int year, int month)
        {
            if (month == 1)
                return (year - 1, 12);
            return (year, month - 1);
        }

        public (int Year, int Month) GetNextMonth(int year, int month)
        {
            if (month == 12)
                return (year + 1, 1);
            return (year, month + 1);
        }
    }
}
