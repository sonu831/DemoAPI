using System.ComponentModel.DataAnnotations;

namespace SampleWebAPI.Models
{
    public class SystemInfo
    {
        public string PodName { get; set; } = string.Empty;
        public string PodNamespace { get; set; } = string.Empty;
        public string PodIP { get; set; } = string.Empty;
        public string NodeName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceHost { get; set; } = string.Empty;
        public string ServicePort { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;
        public string Environment { get; set; } = string.Empty;
        public string AppVersion { get; set; } = string.Empty;
        public string Framework { get; set; } = string.Empty;
        public string DatabaseServer { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string DatabaseUser { get; set; } = string.Empty;
        public string DatabaseStatus { get; set; } = string.Empty;
        public string? DatabaseVersion { get; set; }
        public int StudentCount { get; set; }
        public int CourseCount { get; set; }
        public int EnrollmentCount { get; set; }
        public int DepartmentCount { get; set; }
        public string ContainerName { get; set; } = string.Empty;
        public bool IsRunningInContainer { get; set; }
        public DateTime ServerTime { get; set; }
        public string ServerTimeZone { get; set; } = string.Empty;
        public ClusterInfo? ClusterInfo { get; set; }
    }

    public class ClusterInfo
    {
        public int TotalPods { get; set; }
        public int RunningPods { get; set; }
        public int PendingPods { get; set; }
        public int FailedPods { get; set; }
        public int TotalServices { get; set; }
        public int TotalNodes { get; set; }
        public int TotalDeployments { get; set; }
        public List<PodInfo> Pods { get; set; } = new();
        public List<ServiceInfo> Services { get; set; } = new();
        public List<NodeInfo> Nodes { get; set; } = new();
        public List<DeploymentInfo> Deployments { get; set; } = new();
        public string? Error { get; set; }
    }

    public class PodInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? PodIP { get; set; }
        public string? NodeName { get; set; }
    }

    public class ServiceInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? ClusterIP { get; set; }
        public int Port { get; set; }
    }

    public class NodeInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? KubernetesVersion { get; set; }
        public string? OSImage { get; set; }
    }

    public class DeploymentInfo
    {
        public string Name { get; set; } = string.Empty;
        public int Replicas { get; set; }
        public int ReadyReplicas { get; set; }
    }
}