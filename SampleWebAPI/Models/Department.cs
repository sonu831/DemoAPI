using System.ComponentModel.DataAnnotations;

namespace SampleWebAPI.Models
{
    public class Department
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string DepartmentName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? DepartmentCode { get; set; }

        public string? Location { get; set; }

        public string? HeadOfDepartment { get; set; }
    }
}