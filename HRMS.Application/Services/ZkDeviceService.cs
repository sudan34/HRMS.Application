using HRMS.Application.Data;
using HRMS.Application.Models;
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
        private readonly IAttendanceStatusService _attendanceStatusService;
        private CZKEMClass _zkDevice;
        private bool _isConnected = false;
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

            // Get all employees (active and inactive) to handle historical records
            var enrollNumbers = validRecords.Select(r => r.enrollNumber).Distinct().ToList();
            var employees = await _context.Employees
                .Where(e => enrollNumbers.Contains(e.EmployeeId))
                .AsNoTracking()
                .ToDictionaryAsync(e => e.EmployeeId);

            // Process all records
            foreach (var record in validRecords)
            {
                try
                {
                    if (!employees.TryGetValue(record.enrollNumber, out var employee))
                    {
                        // Handle unknown employee
                        await HandleUnknownEmployeeRecord(record.enrollNumber, record.recordTime, record.inOutMode);
                        _logger.LogWarning($"Employee not found: {record.enrollNumber}");
                        continue;
                    }

                    if (!employee.IsActive && record.recordTime.Date >= employee.ResignDate?.Date)
                    {
                        // Skip records after resignation date
                        _logger.LogInformation($"Skipping record for inactive employee {employee.EmployeeId} ({employee.FullName}) on {record.recordTime}");
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
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing record for enrollment {record.enrollNumber} at {record.recordTime}");
                }
            }
        }
        private async Task HandleUnknownEmployeeRecord(string enrollNumber, DateTime recordTime, int inOutMode)
        {
            // Option 1: Log and ignore (recommended for unknown employees)
            _logger.LogWarning($"Unknown employee enrollment ID: {enrollNumber} at {recordTime}");

            // Option 2: Create a placeholder record(if you need to track these)
            _context.UnknownAttendanceRecords.Add(new UnknownAttendanceRecord
            {
                EnrollmentNumber = enrollNumber,
                RecordTime = recordTime,
                InOutMode = inOutMode,
                Processed = false
            });
            await _context.SaveChangesAsync();
        }
        private async Task ProcessCheckIn(Employee employee, DateTime checkInTime)
        {
            // Skip if employee was inactive at check-in time
            if (!employee.IsActive && checkInTime.Date >= employee.ResignDate?.Date)
            {
                _logger.LogInformation($"Skipping check-in for inactive employee {employee.EmployeeId} on {checkInTime}");
                return;
            }

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

            if (attendance == null) return;

            // Get department working hours
            var department = await _context.Departments
                .Include(d => d.WorkingHours)
                .FirstOrDefaultAsync(d => d.Id == employee.DepartmentId);

            var isFriday = checkOutTime.DayOfWeek == DayOfWeek.Friday;
            var workingHours = department?.WorkingHours;

            attendance.CheckOut = checkOutTime;

            // Only check early departure on non-Fridays
            if (!isFriday)
            {
                var endTime = workingHours?.EndTime ?? new TimeSpan(17, 0, 0);

                if (checkOutTime.TimeOfDay < endTime.Add(TimeSpan.FromMinutes(-15)))
                {
                    attendance.Status = AttendanceStatus.EarlyDeparture;
                }
            }

            // Finalize status (this will handle Friday's 4-hour requirement automatically)
            attendance.Status = await _attendanceStatusService.FinalizeStatusAsync(attendance);
            await _context.SaveChangesAsync();
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
               
    }
}
