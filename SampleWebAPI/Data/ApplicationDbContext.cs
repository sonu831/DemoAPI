using Microsoft.EntityFrameworkCore;
using SampleWebAPI.Models;

namespace SampleWebAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Department> Departments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and constraints
            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                entity.HasIndex(e => e.Email).IsUnique();
            });

            modelBuilder.Entity<Course>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CourseName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.CourseCode).IsRequired().HasMaxLength(20);
                entity.HasIndex(e => e.CourseCode).IsUnique();
            });

            modelBuilder.Entity<Enrollment>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configure relationships
                entity.HasOne(e => e.Student)
                    .WithMany(s => s.Enrollments)
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Course)
                    .WithMany(c => c.Enrollments)
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DepartmentName).IsRequired().HasMaxLength(100);
            });

            // Seed initial data
            modelBuilder.Entity<Course>().HasData(
                new Course { Id = 1, CourseName = "Computer Science", CourseCode = "CS101", Credits = 3 },
                new Course { Id = 2, CourseName = "Mathematics", CourseCode = "MATH101", Credits = 4 },
                new Course { Id = 3, CourseName = "Physics", CourseCode = "PHY101", Credits = 3 }
            );

            modelBuilder.Entity<Department>().HasData(
                new Department { Id = 1, DepartmentName = "Computer Science", DepartmentCode = "CS", Location = "Building A", HeadOfDepartment = "Dr. Smith" },
                new Department { Id = 2, DepartmentName = "Mathematics", DepartmentCode = "MATH", Location = "Building B", HeadOfDepartment = "Dr. Johnson" },
                new Department { Id = 3, DepartmentName = "Physics", DepartmentCode = "PHY", Location = "Building C", HeadOfDepartment = "Dr. Williams" },
                new Department { Id = 4, DepartmentName = "Engineering", DepartmentCode = "ENG", Location = "Building D", HeadOfDepartment = "Dr. Brown" }
            );

            modelBuilder.Entity<Student>().HasData(
                new Student
                {
                    Id = 1,
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@university.com",
                    DateOfBirth = new DateTime(2000, 5, 15),
                    EnrollmentDate = new DateTime(2024, 9, 1),
                    PhoneNumber = "555-0101",
                    Address = "123 Main St, City, State"
                },
                new Student
                {
                    Id = 2,
                    FirstName = "Jane",
                    LastName = "Smith",
                    Email = "jane.smith@university.com",
                    DateOfBirth = new DateTime(2001, 3, 22),
                    EnrollmentDate = new DateTime(2024, 9, 1),
                    PhoneNumber = "555-0102",
                    Address = "456 Oak Ave, City, State"
                },
                new Student
                {
                    Id = 3,
                    FirstName = "Mike",
                    LastName = "Johnson",
                    Email = "mike.johnson@university.com",
                    DateOfBirth = new DateTime(1999, 11, 8),
                    EnrollmentDate = new DateTime(2023, 9, 1),
                    PhoneNumber = "555-0103",
                    Address = "789 Pine Rd, City, State"
                },
                new Student
                {
                    Id = 4,
                    FirstName = "Emily",
                    LastName = "Davis",
                    Email = "emily.davis@university.com",
                    DateOfBirth = new DateTime(2002, 7, 30),
                    EnrollmentDate = new DateTime(2024, 9, 1),
                    PhoneNumber = "555-0104",
                    Address = "321 Elm St, City, State"
                }
            );

            modelBuilder.Entity<Enrollment>().HasData(
                // John Doe's enrollments
                new Enrollment { Id = 1, StudentId = 1, CourseId = 1, EnrollmentDate = new DateTime(2024, 9, 1), Grade = "A", Status = "Active" },
                new Enrollment { Id = 2, StudentId = 1, CourseId = 2, EnrollmentDate = new DateTime(2024, 9, 1), Grade = "B+", Status = "Active" },

                // Jane Smith's enrollments
                new Enrollment { Id = 3, StudentId = 2, CourseId = 1, EnrollmentDate = new DateTime(2024, 9, 1), Grade = "A-", Status = "Active" },
                new Enrollment { Id = 4, StudentId = 2, CourseId = 3, EnrollmentDate = new DateTime(2024, 9, 1), Grade = "B", Status = "Active" },

                // Mike Johnson's enrollments
                new Enrollment { Id = 5, StudentId = 3, CourseId = 2, EnrollmentDate = new DateTime(2023, 9, 1), Grade = "A", Status = "Completed" },
                new Enrollment { Id = 6, StudentId = 3, CourseId = 3, EnrollmentDate = new DateTime(2024, 1, 15), Grade = null, Status = "Active" },

                // Emily Davis's enrollments
                new Enrollment { Id = 7, StudentId = 4, CourseId = 1, EnrollmentDate = new DateTime(2024, 9, 1), Grade = null, Status = "Active" },
                new Enrollment { Id = 8, StudentId = 4, CourseId = 2, EnrollmentDate = new DateTime(2024, 9, 1), Grade = null, Status = "Active" }
            );
        }
    }
}