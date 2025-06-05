using HRMS.Application.Data;
using HRMS.Application.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;
using zkemkeeper;

namespace HRMS.Application.Services
{
    public class ZkDeviceService : IZkDeviceService, IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ZkDeviceService> _logger;
        private readonly IConfiguration _config;
        private CZKEMClass _zkDevice;
        private bool _isConnected = false;
        private readonly Dictionary<int, TimeSpan> _departmentLateTimes;
        public ZkDeviceService(ApplicationDbContext context,
                             ILogger<ZkDeviceService> logger,
                             IConfiguration config)
        {
            _context = context;
            _logger = logger;
            _config = config;
            _zkDevice = new CZKEMClass();
            _departmentLateTimes = LoadDepartmentLateTimes();
        }

        private Dictionary<int, TimeSpan> LoadDepartmentLateTimes()
        {
            var lateTimes = new Dictionary<int, TimeSpan>();
            var defaultTime = TimeSpan.Parse(_config["DepartmentSettings:LateTimes:Default"]);

            // Load all configured department late times
            var section = _config.GetSection("DepartmentSettings:LateTimes");
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
            var enrollNumbers = records.Select(r => r.enrollNumber).Distinct().ToList();
            var employees = await _context.Employees
                .Include(e => e.Department)
                .ThenInclude(d => d.DepartmentWeekend)
                .Where(e => enrollNumbers.Contains(e.EmployeeId))
                .ToDictionaryAsync(e => e.EmployeeId);  // Key by EmployeeId now

            var attendancesToAdd = new List<Attendance>();
            var attendancesToUpdate = new List<Attendance>();

           // var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

            foreach (var record in records)
            {
                if (!employees.TryGetValue(record.enrollNumber, out var employee))
                {
                    _logger.LogWarning($"Employee not found with Enrollment ID: {record.enrollNumber}");
                    continue;
                }

                var isWeekend = IsWeekendForDepartment(employee.Department, record.recordTime);
                if (isWeekend)
                {
                    // Skip processing or mark as weekend attendance
                    continue; // or create a weekend attendance record
                }

                if (record.inOutMode == 0) // Check-in
                {
                    var existing = await _context.Attendances
                        .FirstOrDefaultAsync(a =>
                            a.EmployeeId == employee.EmployeeId &&  // Use EmployeeId here
                            a.CheckIn.Date == record.recordTime.Date);

                    if (existing == null)
                    {
                        attendancesToAdd.Add(new Attendance
                        {
                            EmployeeId = employee.EmployeeId,  // Use EmployeeId here
                            CheckIn = record.recordTime,
                            Status = DetermineAttendanceStatus(employee.DepartmentId, record.recordTime),
                            CreatedBy = "System",
                            CreatedOn = DateTime.Now
                        });
                    }
                }
                else // Check-out
                {
                    var attendance = await _context.Attendances
                        .Where(a =>
                            a.EmployeeId == employee.EmployeeId &&  // Use EmployeeId here
                            a.CheckIn.Date == record.recordTime.Date)
                        .OrderByDescending(a => a.CheckIn)
                        .FirstOrDefaultAsync();

                    if (attendance != null && attendance.CheckOut == null)
                    {
                        attendance.CheckOut = record.recordTime;
                        attendance.UpdatedBy = "System";
                        attendance.UpdatedOn = DateTime.Now;
                        attendancesToUpdate.Add(attendance);
                    }
                }
            }

            if (attendancesToAdd.Any())
            {
                await _context.Attendances.AddRangeAsync(attendancesToAdd);
                await _context.SaveChangesAsync();
            }

            if (attendancesToUpdate.Any())
            {
                _context.Attendances.UpdateRange(attendancesToUpdate);
                await _context.SaveChangesAsync();
            }
        }
        private AttendanceStatus DetermineAttendanceStatus(int departmentId, DateTime checkInTime)
        {
            var lateTime = _departmentLateTimes.TryGetValue(departmentId, out var deptTime)
                 ? deptTime
                 : TimeSpan.Parse(_config["DepartmentSettings:LateTimes:Default"] ?? "09:30:00");

            return checkInTime.TimeOfDay > lateTime
                ? AttendanceStatus.Late
                : AttendanceStatus.Present;
        }
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
            if (department?.DepartmentWeekend == null)
            {
                // Default for departments without configuration: Saturday only
                return date.DayOfWeek == DayOfWeek.Saturday;
            }
            var day = date.DayOfWeek;
            var weekend = department.DepartmentWeekend;

            // Check both days if WeekendDay2 is set
            return day == weekend.WeekendDay1 ||
                  (weekend.WeekendDay2.HasValue && day == weekend.WeekendDay2.Value);
        }
    }
}
