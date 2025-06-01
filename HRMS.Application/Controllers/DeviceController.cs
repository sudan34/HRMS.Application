using HRMS.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Application.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceController : ControllerBase
    {
        private readonly IZkDeviceService _deviceService;

        public DeviceController(IZkDeviceService deviceService)
        {
            _deviceService = deviceService;
        }

        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            var isConnected = await _deviceService.TestConnectionAsync();
            return Ok(new { Connected = isConnected });
        }
    }
}
