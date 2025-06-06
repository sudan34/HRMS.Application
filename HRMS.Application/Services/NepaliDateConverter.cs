namespace HRMS.Application.Services
{
    public class NepaliDateConverter : INepaliDateConverter
    {
        // Using data from https://github.com/rabinadk1/NepaliDateConverter.NET
        private static readonly Dictionary<int, int[]> CalendarData = new()
        {
            //{2000, new[] { 30, 32, 31, 32, 31, 30, 30, 30, 29, 30, 29, 31 }},
            {2001, new[] { 31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30 }},

            {2000, new[] { 30, 32, 31, 32, 31, 30, 30, 30, 29, 30, 29, 31 }},
            // Add all required years here
            {2080, new[] { 31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30 }},

            {2081, new[] {31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30}},
            {2082, new[] {31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30}},
            {2083, new[] {31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30}},
            {2084, new[] {31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30}},
            {2085, new[] {31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30}},
        };

        // Reference AD date for conversion calculations
        private static readonly DateTime ReferenceAdDate = new DateTime(1944, 1, 1);
        private static readonly (int Year, int Month, int Day) ReferenceBsDate = (2000, 9, 17);

        public (int Year, int Month, int Day) ConvertToBS(DateTime adDate)
        {
            int totalAdDays = (int)(adDate - ReferenceAdDate).TotalDays;
            int currentBsYear = ReferenceBsDate.Year;
            int currentBsMonth = ReferenceBsDate.Month;
            int currentBsDay = ReferenceBsDate.Day;

            int remainingDays = totalAdDays;

            while (remainingDays > 0)
            {
                int daysInMonth = CalendarData[currentBsYear][currentBsMonth - 1];
                if (remainingDays >= daysInMonth - currentBsDay)
                {
                    remainingDays -= daysInMonth - currentBsDay + 1;
                    currentBsDay = 1;
                    currentBsMonth++;
                    if (currentBsMonth > 12)
                    {
                        currentBsMonth = 1;
                        currentBsYear++;
                    }
                }
                else
                {
                    currentBsDay += remainingDays;
                    remainingDays = 0;
                }
            }

            return (currentBsYear, currentBsMonth, currentBsDay);
        }

        public DateTime ConvertToAD(int bsYear, int bsMonth, int bsDay)
        {
            if (!CalendarData.ContainsKey(bsYear))
            {
                throw new ArgumentException($"Year {bsYear} is not supported.");
            }

            int totalDays = 0;
            int currentYear = ReferenceBsDate.Year;
            int currentMonth = ReferenceBsDate.Month;
            int currentDay = ReferenceBsDate.Day;

            while (currentYear < bsYear || currentMonth < bsMonth || currentDay < bsDay)
            {
                int daysInMonth = CalendarData[currentYear][currentMonth - 1];
                if (currentYear < bsYear || currentMonth < bsMonth)
                {
                    totalDays += daysInMonth - currentDay + 1;
                    currentDay = 1;
                }
                else
                {
                    totalDays += bsDay - currentDay;
                    currentDay = bsDay;
                }

                currentMonth++;
                if (currentMonth > 12)
                {
                    currentMonth = 1;
                    currentYear++;
                }
            }

            return ReferenceAdDate.AddDays(totalDays);
        }

        public bool IsValidBSDate(int year, int month, int day)
        {
            if (!CalendarData.ContainsKey(year) || month < 1 || month > 12)
                return false;

            return day >= 1 && day <= CalendarData[year][month - 1];
        }
    }

}

