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
    }
}