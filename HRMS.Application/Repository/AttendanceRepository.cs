using Dapper;
using HRMS.Application.DTOs;
using HRMS.Application.Models;
using System.Data;

namespace HRMS.Application.Repository
{
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly IDbConnection _db;

        public AttendanceRepository(IDbConnection db)
        {
            _db = db;
        }

        public async Task<List<DepartmentWorkingHours>> GetDepartmentWorkingHoursAsync()
        {
            const string sql = "SELECT * FROM DepartmentWorkingHours";
            return (await _db.QueryAsync<DepartmentWorkingHours>(sql)).ToList();
        }

        public async Task<List<AttendanceReportDto>> GetAttendanceSummaryAsync(DateTime fromDate, DateTime toDate, int batchSizeDays = 30)
        {
            var results = new List<AttendanceReportDto>();

            DateTime currentStart = fromDate;
            while (currentStart <= toDate)
            {
                DateTime currentEnd = currentStart.AddDays(batchSizeDays - 1);
                if (currentEnd > toDate) currentEnd = toDate;

                var batchResults = await GetAttendanceBatchAsync(currentStart, currentEnd);
                results.AddRange(batchResults);

                currentStart = currentEnd.AddDays(1);
            }

            // Merge results for employees who appear in multiple batches
            return results.GroupBy(r => r.EmployeeId)
                          .Select(g => new AttendanceReportDto
                          {
                              EmployeeId = g.Key,
                              FullName = g.First().FullName,
                              Department = g.First().Department,
                              Designation = g.First().Designation,
                              TotalPresent = g.Sum(x => x.TotalPresent),
                              TotalLeave = g.Sum(x => x.TotalLeave),
                              TotalHoliday = g.Sum(x => x.TotalHoliday),
                              TotalWeekend = g.Sum(x => x.TotalWeekend),
                              TotalAbsent = g.Sum(x => x.TotalAbsent)
                          })
                          .OrderBy(r => r.FullName)
                          .ToList();
        }

        private async Task<List<AttendanceReportDto>> GetAttendanceBatchAsync(DateTime fromDate, DateTime toDate)
        {
            const string sql = @"
            -- First filter active employees
            WITH ActiveEmployees AS (
                SELECT 
                    Id,
                    EmployeeId,
                    FirstName,
                    LastName,
                    Designation,
                    DepartmentId
                FROM Employees WITH (NOLOCK)
                WHERE IsActive = 1
            ),

            -- Then join with other tables
            EmployeeDept AS (
                SELECT 
                    ae.Id,
                    ae.EmployeeId,
                    ae.FirstName,
                    ae.LastName,
                    ae.Designation,
                    d.Id AS DepartmentId,
                    d.Name AS Department,
                    dw.StartTime,
                    dw.EndTime,
                    dw.LateThreshold,
                    dw.FridayStartTime,
                    dw.FridayEndTime,
                    dw.FridayLateThreshold
                FROM ActiveEmployees ae
                JOIN Departments d WITH (NOLOCK) ON ae.DepartmentId = d.Id
                LEFT JOIN DepartmentWorkingHours dw WITH (NOLOCK) ON d.Id = dw.DepartmentId
            ),

            -- Efficient date range generation
            DateRange AS (
                SELECT DATEADD(DAY, n, @FromDate) AS Date
                FROM (
                    SELECT TOP (DATEDIFF(DAY, @FromDate, @ToDate) + 1) 
                        ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS n
                    FROM sys.objects a WITH (NOLOCK)
                    CROSS JOIN sys.objects b WITH (NOLOCK)
                    WHERE DATEDIFF(DAY, @FromDate, @ToDate) < 1000 -- Safety limit
                ) AS Numbers
            ),

            -- Get department weekend info
            DeptWeekends AS (
                SELECT 
                    DepartmentId,
                    CAST(WeekendDay1 AS INT) AS WeekendDay1,
                    CASE WHEN WeekendDay2 IS NOT NULL THEN CAST(WeekendDay2 AS INT) ELSE -1 END AS WeekendDay2
                FROM DepartmentWeekends WITH (NOLOCK)
            ),

            -- Get active holidays just for this date range
            ActiveHolidays AS (
                SELECT Date 
                FROM Holidays WITH (NOLOCK)
                WHERE Date BETWEEN @FromDate AND @ToDate
                AND IsActive = 1
            ),

            -- Get approved leaves that overlap with date range
            ApprovedLeaves AS (
                SELECT 
                    EmployeeId,
                    StartDate,
                    EndDate
                FROM Leaves WITH (NOLOCK)
                WHERE Status = 1 -- Approved
                AND StartDate <= @ToDate
                AND EndDate >= @FromDate
            ),

            -- Calculate daily status for each employee
            DailyStatus AS (
                SELECT
                    ed.EmployeeId,
                    ed.FirstName + ' ' + ed.LastName AS FullName,
                    ed.Department,
                    ed.Designation,
                    dr.Date,
                    CASE
                        WHEN ah.Date IS NOT NULL THEN 'Holiday'
                        WHEN EXISTS (
                            SELECT 1 FROM DeptWeekends dw 
                            WHERE dw.DepartmentId = ed.DepartmentId
                            AND (dw.WeekendDay1 = DATEPART(WEEKDAY, dr.Date) OR 
                                 dw.WeekendDay2 = DATEPART(WEEKDAY, dr.Date))
                        ) THEN 'Weekend'
                        WHEN EXISTS (
                            SELECT 1 FROM ApprovedLeaves al 
                            WHERE al.EmployeeId = ed.Id
                            AND dr.Date BETWEEN al.StartDate AND al.EndDate
                        ) THEN 'Leave'
                        WHEN EXISTS (
                            SELECT 1 FROM Attendances a WITH (NOLOCK)
                            WHERE a.EmployeeId = ed.EmployeeId
                            AND CONVERT(DATE, a.CheckIn) = dr.Date
                        ) THEN 'Present'
                        ELSE 'Absent'
                    END AS Status
                FROM EmployeeDept ed
                CROSS JOIN DateRange dr
                LEFT JOIN ActiveHolidays ah ON dr.Date = ah.Date
            ),

            -- Pre-aggregate counts before final join
            DailyCounts AS (
                SELECT
                    EmployeeId,
                    SUM(CASE WHEN Status = 'Present' THEN 1 ELSE 0 END) AS TotalPresent,
                    SUM(CASE WHEN Status = 'Leave' THEN 1 ELSE 0 END) AS TotalLeave,
                    SUM(CASE WHEN Status = 'Holiday' THEN 1 ELSE 0 END) AS TotalHoliday,
                    SUM(CASE WHEN Status = 'Weekend' THEN 1 ELSE 0 END) AS TotalWeekend,
                    SUM(CASE WHEN Status = 'Absent' THEN 1 ELSE 0 END) AS TotalAbsent
                FROM DailyStatus
                GROUP BY EmployeeId
            )

            -- Final join with employee info
            SELECT
                ed.EmployeeId,
                ed.FirstName + ' ' + ed.LastName AS FullName,
                ed.Department,
                ed.Designation,
                ISNULL(dc.TotalPresent, 0) AS TotalPresent,
                ISNULL(dc.TotalLeave, 0) AS TotalLeave,
                ISNULL(dc.TotalHoliday, 0) AS TotalHoliday,
                ISNULL(dc.TotalWeekend, 0) AS TotalWeekend,
                ISNULL(dc.TotalAbsent, 0) AS TotalAbsent
            FROM EmployeeDept ed
            LEFT JOIN DailyCounts dc ON ed.EmployeeId = dc.EmployeeId
            ORDER BY FullName";

            return (await _db.QueryAsync<AttendanceReportDto>(sql,
                new { FromDate = fromDate, ToDate = toDate },
                commandTimeout: 120)).ToList(); // 2 minute timeout per batch
        }

        public async Task<List<DailyAttendanceDto>> GetEmployeeAttendanceAsync(string employeeId, DateTime fromDate, DateTime toDate)
        {
            const string sql = @"
            -- Get employee details with working hours
            WITH EmployeeInfo AS (
                SELECT 
                    e.Id,
                    e.EmployeeId,
                    e.FirstName,
                    e.LastName,
                    e.Designation,
                    d.Id AS DepartmentId,
                    d.Name AS Department,
                    dw.StartTime,
                    dw.EndTime,
                    dw.LateThreshold,
                    dw.FridayStartTime,
                    dw.FridayEndTime,
                    dw.FridayLateThreshold
                FROM Employees e
                JOIN Departments d ON e.DepartmentId = d.Id
                LEFT JOIN DepartmentWorkingHours dw ON d.Id = dw.DepartmentId
                WHERE e.EmployeeId = @EmployeeId
                AND e.IsActive = 1
            ),

            DateRange AS (
                SELECT DATEADD(DAY, number, @FromDate) AS Date
                FROM master.dbo.spt_values
                WHERE type = 'P' 
                AND number <= DATEDIFF(DAY, @FromDate, @ToDate)
            ),

            EmpDeptWeekends AS (
                SELECT 
                    WeekendDay1,
                    ISNULL(WeekendDay2, -1) AS WeekendDay2
                FROM DepartmentWeekends dw
                WHERE EXISTS (
                    SELECT 1 FROM EmployeeInfo ei 
                    WHERE dw.DepartmentId = ei.DepartmentId
                )
            ),

            EmpHolidays AS (
                SELECT Date, Name 
                FROM Holidays
                WHERE Date BETWEEN @FromDate AND @ToDate
                AND IsActive = 1
            ),

            ApprovedLeaves AS (
                SELECT 
                    StartDate,
                    EndDate,
                    CASE 
                        WHEN Type = 0 THEN 'Sick'
                        WHEN Type = 1 THEN 'Vacation'
                        WHEN Type = 2 THEN 'Personal'
                        WHEN Type = 3 THEN 'Maternity'
                        WHEN Type = 4 THEN 'Paternity'
                        WHEN Type = 5 THEN 'Bereavement'
                        WHEN Type = 6 THEN 'Other'
                    END AS LeaveType
                FROM Leaves
                WHERE EmployeeId = (SELECT Id FROM EmployeeInfo)
                AND Status = 1 -- Approved
                AND (
                    (StartDate <= @ToDate AND EndDate >= @FromDate) OR
                    (StartDate BETWEEN @FromDate AND @ToDate) OR
                    (EndDate BETWEEN @FromDate AND @ToDate)
                )
            ),

            EmployeeAttendance AS (
                SELECT 
                    CONVERT(DATE, CheckIn) AS Date,
                    CheckIn,
                    CheckOut,
                    DATEDIFF(MINUTE, CheckIn, CheckOut) / 60.0 AS WorkingHoursDecimal
                FROM Attendances
                WHERE EmployeeId = @EmployeeId
                AND CheckIn BETWEEN @FromDate AND DATEADD(DAY, 1, @ToDate)
            ),

            DailyStatus AS (
                SELECT
                    dr.Date,
                    DATENAME(WEEKDAY, dr.Date) AS DayName,
                    DATEPART(WEEKDAY, dr.Date) AS DayOfWeek,
                    h.Name AS HolidayName,
                    al.LeaveType,
                    ea.CheckIn,
                    ea.CheckOut,
                    ea.WorkingHoursDecimal,
                    CASE
                        WHEN h.Date IS NOT NULL THEN 'Holiday'
                        WHEN EXISTS (
                            SELECT 1 FROM EmpDeptWeekends dw 
                            WHERE dw.WeekendDay1 = DATEPART(WEEKDAY, dr.Date) OR 
                                    dw.WeekendDay2 = DATEPART(WEEKDAY, dr.Date)
                        ) THEN 'Weekend'
                        WHEN ea.CheckIn IS NOT NULL THEN -- Check attendance first
                            CASE
                                WHEN DATEPART(WEEKDAY, dr.Date) = 6 THEN -- Friday
                                    CASE
                                        WHEN CONVERT(TIME, ea.CheckIn) > (SELECT FridayLateThreshold FROM EmployeeInfo) THEN 'Late'
                                        WHEN ea.WorkingHoursDecimal < 4 THEN 'HalfDay'
                                        ELSE 'Present'
                                    END
                                ELSE -- Other weekdays
                                    CASE
                                        WHEN CONVERT(TIME, ea.CheckIn) > (SELECT LateThreshold FROM EmployeeInfo) THEN 'Late'
                                        WHEN ea.WorkingHoursDecimal < 8 THEN 'HalfDay'
                                        ELSE 'Present'
                                    END
                            END
                        WHEN al.StartDate IS NOT NULL THEN 'Leave' -- Only mark as Leave if no attendance
                        ELSE 'Absent'
                    END AS Status,
                    CASE
                        WHEN h.Date IS NOT NULL THEN h.Name
                        WHEN EXISTS (
                            SELECT 1 FROM EmpDeptWeekends dw 
                            WHERE dw.WeekendDay1 = DATEPART(WEEKDAY, dr.Date) OR 
                                    dw.WeekendDay2 = DATEPART(WEEKDAY, dr.Date)
                        ) THEN N'Weekend'
                        WHEN ea.CheckIn IS NULL AND al.StartDate IS NOT NULL THEN CAST(al.LeaveType AS NVARCHAR(100))
                        ELSE NULL
                    END AS StatusReason
                FROM DateRange dr
                LEFT JOIN EmpHolidays h ON dr.Date = h.Date
                LEFT JOIN ApprovedLeaves al ON dr.Date BETWEEN al.StartDate AND al.EndDate
                LEFT JOIN EmployeeAttendance ea ON dr.Date = ea.Date
            )

            SELECT 
                Date,
                DayName AS Day,
                CheckIn,
                CheckOut,
                WorkingHoursDecimal AS WorkingHours,
                CASE
                    WHEN CheckOut IS NOT NULL THEN 
                        CONVERT(VARCHAR, DATEDIFF(HOUR, CheckIn, CheckOut)) + 'h ' + 
                        CONVERT(VARCHAR, DATEDIFF(MINUTE, CheckIn, CheckOut) % 60) + 'm'
                    ELSE NULL
                END AS WorkingHoursDisplay,
                Status,
                StatusReason
            FROM DailyStatus
            ORDER BY Date DESC";

            return (await _db.QueryAsync<DailyAttendanceDto>(sql, new
            {
                EmployeeId = employeeId,
                FromDate = fromDate,
                ToDate = toDate
            })).ToList();
        }
    }
}
