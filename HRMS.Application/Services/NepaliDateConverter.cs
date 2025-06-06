namespace HRMS.Application.Services
{
    public class NepaliDateConverter : INepaliDateConverter
    {
        // Using data from https://github.com/rabinadk1/NepaliDateConverter.NET
        private static readonly Dictionary<int, int[]> CalendarData = new()
        {
            {2000, new[] { 30, 32, 31, 32, 31, 30, 30, 30, 29, 30, 29, 31 }},
            // Add all required years here
            {2080, new[] { 31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30 }},

            {2081, new[] {31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30}},
            {2082, new[] {31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30}},
            {2083, new[] {31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30}},
            {2084, new[] {31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30}},
            {2085, new[] {31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30}},
        };

        public (int Year, int Month, int Day) ConvertToBS(DateTime adDate)
        {
            // Implementation of AD to BS conversion
            // This is a simplified version - use a proper library for production
            // ...
            return (2080, 5, 15); // Example return
        }

        public DateTime ConvertToAD(int bsYear, int bsMonth, int bsDay)
        {
            // Implementation of BS to AD conversion
            // ...
            return DateTime.Now; // Example return
        }

        public bool IsValidBSDate(int year, int month, int day)
        {
            if (!CalendarData.ContainsKey(year) || month < 1 || month > 12)
                return false;

            return day >= 1 && day <= CalendarData[year][month - 1];
        }
    }
}
