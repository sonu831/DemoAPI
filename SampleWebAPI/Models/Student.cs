using System.ComponentModel.DataAnnotations;

namespace SampleWebAPI.Models
{
    public class Student
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }

        public DateTime EnrollmentDate { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        // Navigation property - ADD THIS LINE
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}