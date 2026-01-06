using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Management;
using System.Runtime.Versioning;
using System.ServiceProcess;

namespace WindowsServiceApi.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    [Authorize] // Require authentication for all endpoints
    public class ServiceController : ControllerBase
    {
        private readonly string _serviceName;

        public ServiceController(IConfiguration configuration)
        {
            _serviceName = configuration.GetValue<string>("ApiServiceSettings:ServiceName") ?? "MyWindowsServiceName";
        }

        [SupportedOSPlatform("windows")]
        [HttpGet]
        public IActionResult GetServiceStatus()
        {
            using var serviceController = new System.ServiceProcess.ServiceController(_serviceName);

            try
            {
                var statusText = serviceController.Status == ServiceControllerStatus.Running ? "Running" : "Stopped";
                bool isRunning = serviceController.Status == ServiceControllerStatus.Running;

                return Ok(new { ServiceIsRunning = isRunning, StatusText = statusText });
            }
            catch (InvalidOperationException)
            {
                return NotFound(new { ServiceIsRunning = false, StatusText = "Service not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ServiceIsRunning = false, StatusText = $"Error: {ex.Message}" });
            }
        }

        [SupportedOSPlatform("windows")]
        [HttpPost]
        public IActionResult StartService()
        {
            using var serviceController = new System.ServiceProcess.ServiceController(_serviceName);

            try
            {
                if (serviceController.Status == ServiceControllerStatus.Running)
                {
                    return BadRequest(new { Message = "Service is already running." });
                }

                serviceController.Start();
                serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));

                var resultCode = ChangeStartMode(_serviceName, "automatic");
                if (resultCode != 0)
                    return StatusCode(500, new { Message = $"Failed to change start mode. WMI returned {resultCode}." });

                return Ok(new { Message = "Service started successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Error: {ex.Message}" });
            }
        }

        [SupportedOSPlatform("windows")]
        [HttpPost]
        public IActionResult StopService()
        {
            using var serviceController = new System.ServiceProcess.ServiceController(_serviceName);

            try
            {
                if (serviceController.Status == ServiceControllerStatus.Stopped)
                {
                    return BadRequest(new { Message = "Service is already stopped." });
                }

                serviceController.Stop();
                serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));

                // 2) Change startup type to Manual
                var resultCode = ChangeStartMode(_serviceName, "manual");
                if (resultCode != 0)
                    return StatusCode(500, new { Message = $"Failed to change start mode. WMI returned {resultCode}." });

                return Ok(new { Message = "Service stopped successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Error: {ex.Message}" });
            }
        }

        [SupportedOSPlatform("windows")]
        private static uint ChangeStartMode(string serviceName, string startupType)
        {
            // Normalize and validate the startupType
            if (string.IsNullOrWhiteSpace(startupType))
                throw new ArgumentException("startupType cannot be null or empty.", nameof(startupType));

            string mode = startupType.Trim().ToLowerInvariant() switch
            {
                "manual" => "Manual",
                "automatic" => "Automatic",
                _ => throw new ArgumentException(
                                   $"Invalid startupType '{startupType}'. Valid values are 'manual' or 'automatic'.",
                                   nameof(startupType))
            };

            // Connect to the WMI service object
            using var svcObj = new ManagementObject($"Win32_Service.Name='{serviceName}'");

            // Prepare parameters for ChangeStartMode
            var inParams = svcObj.GetMethodParameters("ChangeStartMode");
            inParams["StartMode"] = mode;

            // Invoke the method and capture the return code (0 = success)
            var outParams = svcObj.InvokeMethod("ChangeStartMode", inParams, null);
            return (uint)(outParams.Properties["ReturnValue"].Value ?? 1);
        }
    }
}

