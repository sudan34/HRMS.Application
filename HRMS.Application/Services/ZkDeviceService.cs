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
        private CZKEMClass _zkDevice;
        private bool _isConnected = false;

        public ZkDeviceService(ApplicationDbContext context,
                             ILogger<ZkDeviceService> logger,
                             IConfiguration config)
        {
            _context = context;
            _logger = logger;
            _config = config;
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
            // var records = await GetAttendanceDataAsync(DateTime.Today.AddDays(-7), DateTime.Today);

            if (!await TestConnectionAsync())
                throw new Exception("ZK Device connection failed");

            try
            {
                int machineNumber = 1; // Default machine number
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
                        var logTime = new DateTime(idwYear, idwMonth, idwDay, idwHour, idwMinute, idwSecond);

                        await ProcessAttendanceRecord(sdwEnrollNumber, logTime, idwInOutMode);
                    }
                }
            }
            finally
            {
                _zkDevice.EnableDevice(1, true);
                Disconnect();
            }
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
        private async Task ProcessAttendanceRecord(string enrollNumber, DateTime recordTime, int inOutMode)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == enrollNumber);

            if (employee == null)
            {
                _logger.LogWarning($"No employee found with ID: {enrollNumber}");
                return;
            }

            // Check if this is a check-in or check-out
            if (inOutMode == 0) // Check-in
            {
                var existing = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id &&
                                            a.CheckIn.Date == recordTime.Date);

                if (existing == null)
                {
                    _context.Attendances.Add(new Attendance
                    {
                        EmployeeId = employee.Id,
                        CheckIn = recordTime,
                        Status = DetermineAttendanceStatus(recordTime)
                    });
                    await _context.SaveChangesAsync();
                }
            }
            else // Check-out
            {
                var attendance = await _context.Attendances
                    .Where(a => a.EmployeeId == employee.Id &&
                               a.CheckIn.Date == recordTime.Date)
                    .OrderByDescending(a => a.CheckIn)
                    .FirstOrDefaultAsync();

                if (attendance != null && attendance.CheckOut == null)
                {
                    attendance.CheckOut = recordTime;
                    await _context.SaveChangesAsync();
                }
            }
        }

        private AttendanceStatus DetermineAttendanceStatus(DateTime checkInTime)
        {
            var lateTime = new TimeSpan(9, 30, 0); // 9:30 AM
            return checkInTime.TimeOfDay > lateTime ?
                AttendanceStatus.Late :
                AttendanceStatus.Present;
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
