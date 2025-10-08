using System.ComponentModel.DataAnnotations;

namespace SampleWebAPI.Models
{
    public class Enrollment
    {
        public int Id { get; set; }

        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;

        public DateTime EnrollmentDate { get; set; }

        [StringLength(2)]
        public string? Grade { get; set; }

        public string Status { get; set; } = "Active"; // Active, Completed, Dropped
    }
}