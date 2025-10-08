using System.ComponentModel.DataAnnotations;

namespace SampleWebAPI.Models
{
    public class Course
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string CourseName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string CourseCode { get; set; } = string.Empty;

        public int Credits { get; set; }

        public string? Description { get; set; }

        // Navigation property - ADD THIS LINE
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}