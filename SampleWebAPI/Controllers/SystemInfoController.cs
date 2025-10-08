using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SampleWebAPI.Data;
using SampleWebAPI.Models;
using System.Net;

namespace SampleWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SystemInfoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public SystemInfoController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<ActionResult<SystemInfo>> GetSystemInfo()
        {
            var systemInfo = new SystemInfo
            {
                PodName = Environment.GetEnvironmentVariable("POD_NAME") ?? "Not in Kubernetes",
                PodNamespace = Environment.GetEnvironmentVariable("POD_NAMESPACE") ?? "Not in Kubernetes",
                PodIP = Environment.GetEnvironmentVariable("POD_IP") ?? GetLocalIPAddress(),
                NodeName = Environment.GetEnvironmentVariable("NODE_NAME") ?? "Not in Kubernetes",
                ServiceName = Environment.GetEnvironmentVariable("SERVICE_NAME") ?? "webapi-service",
                ServiceHost = Environment.GetEnvironmentVariable("WEBAPI_SERVICE_SERVICE_HOST") ?? "localhost",
                ServicePort = Environment.GetEnvironmentVariable("WEBAPI_SERVICE_SERVICE_PORT") ?? "5052",
                HostName = Environment.MachineName,
                Environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Unknown",
                AppVersion = "1.0.0",
                Framework = Environment.Version.ToString(),
                DatabaseServer = _configuration["DB_SERVER"] ?? "Unknown",
                DatabaseName = _configuration["DB_NAME"] ?? "Unknown",
                DatabaseUser = _configuration["DB_USER"] ?? "Unknown",
                ContainerName = Environment.GetEnvironmentVariable("HOSTNAME") ?? Environment.MachineName,
                IsRunningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true",
                ServerTime = DateTime.UtcNow,
                ServerTimeZone = TimeZoneInfo.Local.DisplayName
            };

            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                systemInfo.DatabaseStatus = canConnect ? "Connected" : "Disconnected";

                if (canConnect)
                {
                    var connection = _context.Database.GetDbConnection();
                    systemInfo.DatabaseVersion = connection.ServerVersion;
                    systemInfo.StudentCount = await _context.Students.CountAsync();
                    systemInfo.CourseCount = await _context.Courses.CountAsync();
                    systemInfo.EnrollmentCount = await _context.Enrollments.CountAsync();
                    systemInfo.DepartmentCount = await _context.Departments.CountAsync();
                }
            }
            catch (Exception ex)
            {
                systemInfo.DatabaseStatus = $"Error: {ex.Message}";
            }

            return Ok(systemInfo);
        }

        private string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
                return "No IP found";
            }
            catch
            {
                return "Unable to get IP";
            }
        }
    }

   
}