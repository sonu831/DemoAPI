using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SampleWebAPI.Data;
using System.Net;
using System.Text.Json;
using SampleWebAPI.Models;

namespace SampleWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SystemInfoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public SystemInfoController(
            ApplicationDbContext context,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Get complete system information including Pod, Cluster, Service, and Database details
        /// </summary>
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

            // Get Kubernetes cluster information
            if (systemInfo.PodName != "Not in Kubernetes")
            {
                systemInfo.ClusterInfo = await GetKubernetesClusterInfo();
            }

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

        private async Task<ClusterInfo> GetKubernetesClusterInfo()
        {
            var clusterInfo = new ClusterInfo();

            try
            {
                var token = await System.IO.File.ReadAllTextAsync("/var/run/secrets/kubernetes.io/serviceaccount/token");
                var k8sNamespace = await System.IO.File.ReadAllTextAsync("/var/run/secrets/kubernetes.io/serviceaccount/namespace");

                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };

                var client = new HttpClient(handler);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var k8sApiServer = "https://kubernetes.default.svc";

                // Get Pods in current namespace
                var podsResponse = await client.GetAsync($"{k8sApiServer}/api/v1/namespaces/{k8sNamespace}/pods");
                if (podsResponse.IsSuccessStatusCode)
                {
                    var podsJson = await podsResponse.Content.ReadAsStringAsync();
                    var podsData = JsonDocument.Parse(podsJson);
                    var items = podsData.RootElement.GetProperty("items");

                    clusterInfo.TotalPods = items.GetArrayLength();
                    clusterInfo.RunningPods = 0;
                    clusterInfo.PendingPods = 0;
                    clusterInfo.FailedPods = 0;

                    var podList = new List<PodInfo>();

                    foreach (var pod in items.EnumerateArray())
                    {
                        var podInfo = new PodInfo
                        {
                            Name = pod.GetProperty("metadata").GetProperty("name").GetString() ?? "",
                            Status = pod.GetProperty("status").GetProperty("phase").GetString() ?? "",
                            PodIP = pod.TryGetProperty("status", out var status) &&
                                    status.TryGetProperty("podIP", out var ip) ? ip.GetString() : "N/A",
                            NodeName = pod.TryGetProperty("spec", out var spec) &&
                                      spec.TryGetProperty("nodeName", out var node) ? node.GetString() : "N/A"
                        };

                        if (podInfo.Status == "Running") clusterInfo.RunningPods++;
                        else if (podInfo.Status == "Pending") clusterInfo.PendingPods++;
                        else if (podInfo.Status == "Failed") clusterInfo.FailedPods++;

                        podList.Add(podInfo);
                    }

                    clusterInfo.Pods = podList;
                }

                // Get Services in current namespace
                var servicesResponse = await client.GetAsync($"{k8sApiServer}/api/v1/namespaces/{k8sNamespace}/services");
                if (servicesResponse.IsSuccessStatusCode)
                {
                    var servicesJson = await servicesResponse.Content.ReadAsStringAsync();
                    var servicesData = JsonDocument.Parse(servicesJson);
                    var items = servicesData.RootElement.GetProperty("items");

                    clusterInfo.TotalServices = items.GetArrayLength();

                    var serviceList = new List<ServiceInfo>();
                    foreach (var svc in items.EnumerateArray())
                    {
                        var serviceInfo = new ServiceInfo
                        {
                            Name = svc.GetProperty("metadata").GetProperty("name").GetString() ?? "",
                            Type = svc.GetProperty("spec").GetProperty("type").GetString() ?? "",
                            ClusterIP = svc.TryGetProperty("spec", out var svcSpec) &&
                                       svcSpec.TryGetProperty("clusterIP", out var clusterIp) ?
                                       clusterIp.GetString() : "N/A"
                        };

                        if (svcSpec.TryGetProperty("ports", out var ports) && ports.GetArrayLength() > 0)
                        {
                            var firstPort = ports[0];
                            if (firstPort.TryGetProperty("port", out var port))
                            {
                                serviceInfo.Port = port.GetInt32();
                            }
                        }

                        serviceList.Add(serviceInfo);
                    }

                    clusterInfo.Services = serviceList;
                }

                // Get Nodes
                try
                {
                    var nodesResponse = await client.GetAsync($"{k8sApiServer}/api/v1/nodes");
                    if (nodesResponse.IsSuccessStatusCode)
                    {
                        var nodesJson = await nodesResponse.Content.ReadAsStringAsync();
                        var nodesData = JsonDocument.Parse(nodesJson);
                        var items = nodesData.RootElement.GetProperty("items");

                        clusterInfo.TotalNodes = items.GetArrayLength();

                        var nodeList = new List<NodeInfo>();
                        foreach (var node in items.EnumerateArray())
                        {
                            var nodeInfo = new NodeInfo
                            {
                                Name = node.GetProperty("metadata").GetProperty("name").GetString() ?? ""
                            };

                            if (node.TryGetProperty("status", out var nodeStatus))
                            {
                                if (nodeStatus.TryGetProperty("conditions", out var conditions))
                                {
                                    foreach (var condition in conditions.EnumerateArray())
                                    {
                                        if (condition.GetProperty("type").GetString() == "Ready")
                                        {
                                            nodeInfo.Status = condition.GetProperty("status").GetString() == "True" ? "Ready" : "NotReady";
                                            break;
                                        }
                                    }
                                }

                                if (nodeStatus.TryGetProperty("nodeInfo", out var nodeInfo2))
                                {
                                    nodeInfo.KubernetesVersion = nodeInfo2.TryGetProperty("kubeletVersion", out var ver) ?
                                                                 ver.GetString() : "Unknown";
                                    nodeInfo.OSImage = nodeInfo2.TryGetProperty("osImage", out var os) ?
                                                      os.GetString() : "Unknown";
                                }
                            }

                            nodeList.Add(nodeInfo);
                        }

                        clusterInfo.Nodes = nodeList;
                    }
                }
                catch
                {
                    clusterInfo.TotalNodes = 0;
                    clusterInfo.Nodes = new List<NodeInfo>();
                }

                // Get Deployments
                var deploymentsResponse = await client.GetAsync($"{k8sApiServer}/apis/apps/v1/namespaces/{k8sNamespace}/deployments");
                if (deploymentsResponse.IsSuccessStatusCode)
                {
                    var deploymentsJson = await deploymentsResponse.Content.ReadAsStringAsync();
                    var deploymentsData = JsonDocument.Parse(deploymentsJson);
                    var items = deploymentsData.RootElement.GetProperty("items");

                    clusterInfo.TotalDeployments = items.GetArrayLength();

                    var deploymentList = new List<DeploymentInfo>();
                    foreach (var deploy in items.EnumerateArray())
                    {
                        var deployInfo = new DeploymentInfo
                        {
                            Name = deploy.GetProperty("metadata").GetProperty("name").GetString() ?? "",
                            Replicas = deploy.TryGetProperty("spec", out var depSpec) &&
                                      depSpec.TryGetProperty("replicas", out var reps) ?
                                      reps.GetInt32() : 0,
                            ReadyReplicas = deploy.TryGetProperty("status", out var depStatus) &&
                                           depStatus.TryGetProperty("readyReplicas", out var ready) ?
                                           ready.GetInt32() : 0
                        };

                        deploymentList.Add(deployInfo);
                    }

                    clusterInfo.Deployments = deploymentList;
                }
            }
            catch (Exception ex)
            {
                clusterInfo.Error = $"Unable to fetch cluster info: {ex.Message}";
            }

            return clusterInfo;
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