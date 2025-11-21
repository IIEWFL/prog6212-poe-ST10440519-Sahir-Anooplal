using Microsoft.AspNetCore.Identity;
using CMCS_Part3.Models;

namespace CMCS_Part3.Data
{
    public static class SeedData
    {
        public static async Task Initialize(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // Create roles
            string[] roleNames = { "Lecturer", "ProgrammeCoordinator", "AcademicManager", "HR" }; //[1]

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName); //[1]
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName)); //[1]
                }
            }

            // Create default users for each role
            var users = new[]
            {
                new { Email = "lecturer@cmcs.com", Role = "Lecturer", FirstName = "John", LastName = "Smith" }, //[2]
                new { Email = "coordinator@cmcs.com", Role = "ProgrammeCoordinator", FirstName = "Sarah", LastName = "Johnson" }, //[2]
                new { Email = "manager@cmcs.com", Role = "AcademicManager", FirstName = "Michael", LastName = "Brown" }, //[2]
                new { Email = "hr@cmcs.com", Role = "HR", FirstName = "Emily", LastName = "Davis" } //[2]
            };

            foreach (var userInfo in users)
            {
                var user = await userManager.FindByEmailAsync(userInfo.Email); //[1]
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = userInfo.Email,
                        Email = userInfo.Email,
                        FirstName = userInfo.FirstName,
                        LastName = userInfo.LastName,
                        Role = userInfo.Role,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(user, "123"); //[1]
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, userInfo.Role); //[1]
                    }
                }
            }

            // Seed sample claims
            if (!context.Claims.Any())
            {
                var lecturer = await userManager.FindByEmailAsync("lecturer@cmcs.com"); //[1]

                if (lecturer != null)
                {
                    var claims = new[]
                    {
                        new Models.Claim { LecturerId = lecturer.Id, HoursWorked = 40, HourlyRate = 250, AdditionalNotes = "Regular teaching hours", Status = ClaimStatus.Pending, SubmittedDate = DateTime.UtcNow.AddDays(-5) }, //[2]
                        new Models.Claim { LecturerId = lecturer.Id, HoursWorked = 35, HourlyRate = 280, AdditionalNotes = "Additional marking", Status = ClaimStatus.Approved, SubmittedDate = DateTime.UtcNow.AddDays(-15), ProcessedDate = DateTime.UtcNow.AddDays(-10), ProcessedBy = "coordinator@cmcs.com" }, //[2]
                        new Models.Claim { LecturerId = lecturer.Id, HoursWorked = 45, HourlyRate = 220, AdditionalNotes = "Workshop preparation", Status = ClaimStatus.Rejected, SubmittedDate = DateTime.UtcNow.AddDays(-25), ProcessedDate = DateTime.UtcNow.AddDays(-20), ProcessedBy = "manager@cmcs.com", RejectionReason = "Hours exceed maximum allowed" } //[2]
                    };

                    context.Claims.AddRange(claims); //[2]
                    await context.SaveChangesAsync(); //[2]
                }
            }
        }
    }
}

/*
[1] Microsoft Docs. "ASP.NET Core Identity." https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity
[2] Microsoft Docs. "Entity Framework Core Fundamentals." https://learn.microsoft.com/en-us/ef/core/
*/