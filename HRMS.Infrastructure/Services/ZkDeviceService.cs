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
        private readonly HashSet<string> _knownUnknownEmployees = new();

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
                if (_isConnected) return true;

                string ipAddress = _config["ZkDevice:IpAddress"]!;
                int port = int.Parse(_config["ZkDevice:Port"]!);
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
                    var employeeCache = new Dictionary<string, Employee>();

                    // Batch process records in chunks to reduce memory usage
                    const int batchSize = 1000;
                    int processedCount = 0;

                    while (GetNextRecord(machineNumber, out var record))
                    {
                        records.Add(record);
                        processedCount++;

                        // Process in batches to avoid memory issues
                        if (processedCount % batchSize == 0)
                        {
                            await ProcessBatch(records, employeeCache);
                            records.Clear();
                        }
                    }

                    // Process remaining records
                    if (records.Count > 0)
                    {
                        await ProcessBatch(records, employeeCache);
                    }
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

        private bool GetNextRecord(int machineNumber, out (string enrollNumber, DateTime recordTime, int inOutMode) record)
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

            bool hasData = _zkDevice.SSR_GetGeneralLogData(
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
                ref idwWorkcode);

            if (hasData)
            {
                record = (
                    sdwEnrollNumber,
                    new DateTime(idwYear, idwMonth, idwDay, idwHour, idwMinute, idwSecond),
                    idwInOutMode
                );
                return true;
            }

            record = default;
            return false;
        }

        private async Task ProcessBatch(List<(string enrollNumber, DateTime recordTime, int inOutMode)> records,
                                      Dictionary<string, Employee> employeeCache)
        {
            // Filter out future dates and invalid records
            var currentDate = DateTime.Now.Date;
            var validRecords = records.Where(r => r.recordTime.Date <= currentDate)
                                     .OrderBy(r => r.recordTime)
                                     .ToList();

            if (validRecords.Count == 0) return;

            // Identify new employees we haven't cached yet
            var newEnrollNumbers = validRecords.Select(r => r.enrollNumber)
                .Distinct()
                .Where(id => !employeeCache.ContainsKey(id) && !_knownUnknownEmployees.Contains(id))
                .ToList();

            // Bulk fetch new employees in one query
            if (newEnrollNumbers.Count > 0)
            {
                var newEmployees = await _context.Employees
                    .Where(e => newEnrollNumbers.Contains(e.EmployeeId))
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var employee in newEmployees)
                {
                    employeeCache[employee.EmployeeId] = employee;
                }

                // Track unknown employees to avoid repeated warnings and DB checks
                var unknownEmployees = newEnrollNumbers.Except(newEmployees.Select(e => e.EmployeeId));
                foreach (var unknownId in unknownEmployees)
                {
                    _knownUnknownEmployees.Add(unknownId);
                    _logger.LogWarning($"Employee not found with Enrollment ID: {unknownId}");
                }
            }

            // Process all records
            foreach (var record in validRecords)
            {
                try
                {
                    // Skip known unknown employees
                    if (_knownUnknownEmployees.Contains(record.enrollNumber))
                        continue;

                    if (!employeeCache.TryGetValue(record.enrollNumber, out var employee))
                    {
                        continue;
                    }

                    if (!employee.IsActive && record.recordTime.Date >= employee.ResignDate?.Date)
                    {
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

        private async Task ProcessCheckIn(Employee employee, DateTime checkInTime)
        {
            // Skip if employee was inactive at check-in time
            if (!employee.IsActive && checkInTime.Date >= employee.ResignDate?.Date)
            {
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
                    while (GetNextRecord(machineNumber, out var record))
                    {
                        if (record.recordTime >= fromDate && record.recordTime <= toDate)
                        {
                            records.Add(new AttendanceRecord
                            {
                                UserId = record.enrollNumber,
                                DateTime = record.recordTime,
                                VerifyMode = 0, // Not captured in GetNextRecord
                                InOutMode = record.inOutMode,
                                Status = record.inOutMode == 0 ? "CheckIn" : "CheckOut"
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Marshal.ReleaseComObject(_zkDevice);
            }
            GC.SuppressFinalize(this);
        }
    }
}