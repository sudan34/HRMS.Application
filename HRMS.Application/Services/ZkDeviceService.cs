using HRMS.Application.Data;
using HRMS.Application.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using zkemkeeper;
using static HRMS.Application.Services.AttendanceStatusService;

namespace HRMS.Application.Services
{
    public class ZkDeviceService : IZkDeviceService, IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ZkDeviceService> _logger;
        private readonly IConfiguration _config;
        private readonly IAttendanceStatusService _attendanceStatusService;
        private CZKEMClass _zkDevice;
        private bool _isConnected = false;
        //private readonly Dictionary<int, TimeSpan> _departmentLateTimes;
        public ZkDeviceService(ApplicationDbContext context,
                             ILogger<ZkDeviceService> logger,
                             IConfiguration config,
                              IAttendanceStatusService attendanceStatusService)
        {
            _context = context;
            _logger = logger;
            _config = config;
            _attendanceStatusService = attendanceStatusService;
            _zkDevice = new CZKEMClass();
            //  _departmentLateTimes = LoadDepartmentLateTimes();
        }

        private Dictionary<int, TimeSpan> LoadDepartmentLateTimes()
        {
            var lateTimes = new Dictionary<int, TimeSpan>();

            var defaultTime = TimeSpan.Parse(_config["DepartmentSettings:LateTimes:Default"]);

            var section = _config.GetSection("DepartmentSettings:LateTimes");

            // Load all configured department late times
            foreach (var dept in section.GetChildren())
            {
                if (dept.Key != "Default" &&
                    int.TryParse(dept.Key, out int deptId) &&
                    TimeSpan.TryParse(dept.Value, out TimeSpan time))
                {
                    if (!lateTimes.ContainsKey(deptId))
                    {
                        lateTimes.Add(deptId, time);
                    }
                    else
                    {
                        _logger.LogWarning($"Duplicate department ID in config: {deptId}");
                    }
                }
            }

            return lateTimes;
        }
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                string ipAddress = _config["ZkDevice:IpAddress"];
                int port = int.Parse(_config["ZkDevice:Port"]);
                _isConnected = _zkDevice.Connect_Net(ipAddress, port);
                return _isConnected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZK Device connection error");
                _isConnected = false;
                return false;
            }
        }

        public async Task SyncAttendanceDataAsync()
        {
            if (!await TestConnectionAsync())
                throw new Exception("ZK Device connection failed");

            try
            {
                int machineNumber = 1; // Default machine number
                _zkDevice.EnableDevice(machineNumber, false);

                if (_zkDevice.ReadGeneralLogData(machineNumber))
                {
                    var records = new List<(string, DateTime, int)>();

                    string sdwEnrollNumber = "";
                    int idwVerifyMode = 0;
                    int idwInOutMode = 0;
                    int idwYear = 0;
                    int idwMonth = 0;
                    int idwDay = 0;
                    int idwHour = 0;
                    int idwMinute = 0;
                    int idwSecond = 0;
                    int idwWorkcode = 0;

                    while (_zkDevice.SSR_GetGeneralLogData(
                        machineNumber,
                        out sdwEnrollNumber,
                        out idwVerifyMode,
                        out idwInOutMode,
                        out idwYear,
                        out idwMonth,
                        out idwDay,
                        out idwHour,
                        out idwMinute,
                        out idwSecond,
                        ref idwWorkcode))
                    {
                        var logTime = new DateTime(idwYear, idwMonth, idwDay, idwHour, idwMinute, idwSecond);
                        records.Add((sdwEnrollNumber, logTime, idwInOutMode));
                    }

                    await ProcessAttendanceRecords(records);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing attendance data");
                throw;
            }
            finally
            {
                _zkDevice.EnableDevice(1, true);
                Disconnect();
            }
        }

        private async Task ProcessAttendanceRecords(List<(string enrollNumber, DateTime recordTime, int inOutMode)> records)
        {
            // Filter out future dates
            var currentDate = DateTime.Now.Date;
            var validRecords = records.Where(r => r.recordTime.Date <= currentDate)
                                     .OrderBy(r => r.recordTime)
                                     .ToList();

            var enrollNumbers = validRecords.Select(r => r.enrollNumber).Distinct().ToList();
            var employees = await _context.Employees
                .Where(e => enrollNumbers.Contains(e.EmployeeId))
                .AsNoTracking()
                .ToDictionaryAsync(e => e.EmployeeId);

            // Process all records
            foreach (var record in validRecords)
            {
                if (!employees.TryGetValue(record.enrollNumber, out var employee))
                {
                    _logger.LogWarning($"Employee not found: {record.enrollNumber}");
                    continue;
                }

                if (record.inOutMode == 0) // Check-in
                {
                    await ProcessCheckIn(employee, record.recordTime);
                }
                else // Check-out
                {
                    await ProcessCheckOut(employee, record.recordTime);
                }
            }
        }

        private async Task ProcessCheckIn(Employee employee, DateTime checkInTime)
        {
            var existing = await _context.Attendances
                .FirstOrDefaultAsync(a =>
                    a.EmployeeId == employee.EmployeeId &&
                    a.CheckIn.Date == checkInTime.Date);

            if (existing == null)
            {
                var status = await _attendanceStatusService.DetermineStatusAsync(employee, checkInTime);

                _context.Attendances.Add(new Attendance
                {
                    EmployeeId = employee.EmployeeId,
                    CheckIn = checkInTime,
                    Status = status,
                    CreatedBy = "System",
                    CreatedOn = checkInTime
                });
                await _context.SaveChangesAsync();
            }
        }

        private async Task ProcessCheckOut(Employee employee, DateTime checkOutTime)
        {
            // Find the most recent check-in without a check-out on the same day
            var attendance = await _context.Attendances
                .Where(a => a.EmployeeId == employee.EmployeeId &&
                           a.CheckIn.Date == checkOutTime.Date &&
                           a.CheckOut == null)
                .OrderByDescending(a => a.CheckIn)
                .FirstOrDefaultAsync();

            if (attendance != null)
            {
                // Validate check-out is after check-in
                if (checkOutTime > attendance.CheckIn)
                {
                    attendance.CheckOut = checkOutTime;
                    attendance.Status = await _attendanceStatusService.FinalizeStatusAsync(attendance);
                    attendance.UpdatedBy = "System";
                    attendance.UpdatedOn = checkOutTime;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    _logger.LogWarning($"Invalid check-out time {checkOutTime} before check-in {attendance.CheckIn} for employee {employee.EmployeeId}");
                }
            }
            else
            {
                // Handle check -out without check-in
                var status = await _attendanceStatusService.DetermineStatusAsync(employee, checkOutTime.AddHours(-1));

                _context.Attendances.Add(new Attendance
                {
                    EmployeeId = employee.EmployeeId,
                    CheckIn = checkOutTime.AddHours(-1), // Default 1 hour before check-out
                    CheckOut = checkOutTime,
                    Status = status == AttendanceStatus.OnLeave ? status : AttendanceStatus.Present,
                    CreatedBy = "System",
                    CreatedOn = checkOutTime
                });
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Created new attendance with check-out only for {employee.EmployeeId} at {checkOutTime}");
            }
        }
        //private AttendanceStatus DetermineAttendanceStatus(int departmentId, DateTime checkInTime)
        //{
        //    var lateTime = _departmentLateTimes.TryGetValue(departmentId, out var deptTime)
        //         ? deptTime
        //         : TimeSpan.Parse(_config["DepartmentSettings:LateTimes:Default"] ?? "09:30:00");

        //    return checkInTime.TimeOfDay > lateTime
        //        ? AttendanceStatus.Late
        //        : AttendanceStatus.Present;
        //}
        public async Task<List<AttendanceRecord>> GetAttendanceDataAsync(DateTime fromDate, DateTime toDate)
        {
            var records = new List<AttendanceRecord>();

            if (!await TestConnectionAsync())
                throw new Exception("Failed to connect to ZK device");

            int machineNumber = 1;
            try
            {
                _zkDevice.EnableDevice(machineNumber, false);

                if (_zkDevice.ReadGeneralLogData(machineNumber))
                {
                    string sdwEnrollNumber = "";
                    int idwVerifyMode = 0;
                    int idwInOutMode = 0;
                    int idwYear = 0;
                    int idwMonth = 0;
                    int idwDay = 0;
                    int idwHour = 0;
                    int idwMinute = 0;
                    int idwSecond = 0;
                    int idwWorkcode = 0;

                    while (_zkDevice.SSR_GetGeneralLogData(
                        machineNumber,
                        out sdwEnrollNumber,
                        out idwVerifyMode,
                        out idwInOutMode,
                        out idwYear,
                        out idwMonth,
                        out idwDay,
                        out idwHour,
                        out idwMinute,
                        out idwSecond,
                        ref idwWorkcode))
                    {
                        var recordTime = new DateTime(idwYear, idwMonth, idwDay, idwHour, idwMinute, idwSecond);

                        if (recordTime >= fromDate && recordTime <= toDate)
                        {
                            records.Add(new AttendanceRecord
                            {
                                UserId = sdwEnrollNumber,
                                DateTime = recordTime,
                                VerifyMode = idwVerifyMode,
                                InOutMode = idwInOutMode,
                                Status = idwInOutMode == 0 ? "CheckIn" : "CheckOut"
                            });
                        }
                    }
                }
                return records;
            }
            finally
            {
                _zkDevice.EnableDevice(machineNumber, true);
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (_isConnected)
            {
                _zkDevice.Disconnect();
                _isConnected = false;
            }
        }

        public void Dispose()
        {
            Disconnect();
            if (_zkDevice != null)
            {
                Marshal.ReleaseComObject(_zkDevice);
                _zkDevice = null;
            }
            GC.SuppressFinalize(this);
        }

        private bool IsWeekendForDepartment(Department department, DateTime date)
        {
            try
            {
                if (department?.DepartmentWeekend == null)
                {
                    // Default for departments without configuration: Saturday only
                    bool isDefaultWeekend = date.DayOfWeek == DayOfWeek.Saturday;
                    _logger.LogTrace($"No weekend config for {department?.Name}, using default: {isDefaultWeekend}");
                    return isDefaultWeekend;
                }
                //    {
                //    // Default for departments without configuration: Saturday only
                //    return date.DayOfWeek == DayOfWeek.Saturday;
                //}
                var day = date.DayOfWeek;
                var weekend = department.DepartmentWeekend;

                bool isWeekend = day == weekend.WeekendDay1 ||
                            (weekend.WeekendDay2.HasValue && day == weekend.WeekendDay2.Value);

                _logger.LogTrace($"Weekend check for {department.Name}: {date:yyyy-MM-dd} (Day {day}) " +
                                $"vs config: {weekend.WeekendDay1}/{weekend.WeekendDay2} = {isWeekend}");

                return isWeekend;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking weekend for department {department?.Id}");
                return false; // Don't skip records if there's an error
            }
        }
    }
}
