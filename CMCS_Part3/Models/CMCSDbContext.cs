using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace CMCS_Part3.Models
{
    public class CMCSDbContext : DbContext
    {
        public CMCSDbContext(DbContextOptions<CMCSDbContext> options) : base(options) { }

        public DbSet<Lecturer> Lecturers { get; set; }
        public DbSet<Claim> Claims { get; set; }
        public DbSet<SupportingDocument> SupportingDocuments { get; set; }
        public DbSet<Report> Reports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Seed example lecturers with enhanced data
            modelBuilder.Entity<Lecturer>().HasData(
                new Lecturer
                {
                    Id = 1,
                    Name = "Dr. Tom Hanson",
                    Email = "tom.hanson@university.com",
                    PhoneNumber = "+27 11 123 4567",
                    Department = "Computer Science"
                },
                new Lecturer
                {
                    Id = 2,
                    Name = "Prof. Sarah Jane",
                    Email = "sarah.jane@university.com",
                    PhoneNumber = "+27 11 123 4568",
                    Department = "Information Technology"
                }
            );

            // Seed sample claims
            modelBuilder.Entity<Claim>().HasData(
                new Claim
                {
                    Id = 1,
                    LecturerId = 1,
                    ClaimMonth = "2025-01",
                    HoursWorked = 40,
                    HourlyRate = 550.00m,
                    Notes = "Guest lectures for PROG6212",
                    Status = ClaimStatus.Pending
                },
                new Claim
                {
                    Id = 2,
                    LecturerId = 2,
                    ClaimMonth = "2025-01",
                    HoursWorked = 35,
                    HourlyRate = 600.00m,
                    Notes = "Course development and marking",
                    Status = ClaimStatus.Approved,
                    ApprovalDate = DateTime.Now.AddDays(-2),
                    ApprovedBy = "Coordinator"
                }
            );
        }
    }
}