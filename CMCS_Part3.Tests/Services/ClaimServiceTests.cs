using CMCS_Part3.Data;
using CMCS_Part3.Models;
using CMCS_Part3.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CMCS_Part3.Tests.Services
{
    public class ClaimServiceTests
    {
        private ApplicationDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>() //[2]
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) //[2]
                .Options;

            return new ApplicationDbContext(options); //[1]
        }

        [Fact]
        public async Task SubmitClaim_ValidClaim_ReturnsSuccess()
        {
            // Arrange
            using var context = GetDbContext(); //[1]
            var environment = new Mock<IWebHostEnvironment>();
            environment.Setup(e => e.WebRootPath).Returns("wwwroot");

            var service = new ClaimService(context, environment.Object); //[1]
            var claim = new Claim
            {
                LecturerId = "test-lecturer-1",
                HoursWorked = 40,
                HourlyRate = 200
            };

            // Act
            var result = await service.SubmitClaimAsync(claim, null); //[1]

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, await context.Claims.CountAsync()); //[2]
        }

        [Fact]
        public async Task SubmitClaim_TooManyHours_ReturnsFailure()
        {
            // Arrange
            using var context = GetDbContext(); //[1]
            var environment = new Mock<IWebHostEnvironment>();
            environment.Setup(e => e.WebRootPath).Returns("wwwroot");

            var service = new ClaimService(context, environment.Object); //[1]
            var claim = new Claim
            {
                LecturerId = "test-lecturer-1",
                HoursWorked = 200, // Too many hours
                HourlyRate = 200
            };

            // Act
            var result = await service.SubmitClaimAsync(claim, null); //[1]

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Maximum 176 hours", result.ErrorMessage);
        }

        [Fact]
        public async Task SubmitClaim_InvalidRate_ReturnsFailure()
        {
            // Arrange
            using var context = GetDbContext(); //[1]
            var environment = new Mock<IWebHostEnvironment>();
            environment.Setup(e => e.WebRootPath).Returns("wwwroot");

            var service = new ClaimService(context, environment.Object); //[1]
            var claim = new Claim
            {
                LecturerId = "test-lecturer-1",
                HoursWorked = 40,
                HourlyRate = 50 // Rate too low
            };

            // Act
            var result = await service.SubmitClaimAsync(claim, null); //[1]

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Hourly rate must be between R100 and R1000", result.ErrorMessage);
        }

        [Fact]
        public async Task GetPendingClaims_NoPendingClaims_ReturnsEmptyList()
        {
            // Arrange
            using var context = GetDbContext(); //[1]
            var environment = new Mock<IWebHostEnvironment>();
            environment.Setup(e => e.WebRootPath).Returns("wwwroot");

            var service = new ClaimService(context, environment.Object); //[1]

            // Act
            var result = await service.GetPendingClaimsAsync();

            // Assert
            Assert.Empty(result);
        }


        [Fact]
        public async Task ApproveClaim_NonExistentClaim_ReturnsFailure()
        {
            // Arrange
            using var context = GetDbContext(); //[1]
            var environment = new Mock<IWebHostEnvironment>();
            environment.Setup(e => e.WebRootPath).Returns("wwwroot");

            var service = new ClaimService(context, environment.Object); //[1]

            // Act
            var result = await service.ApproveClaimAsync(999, "test-approver"); // Non-existent ID

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Claim not found", result.ErrorMessage);
        }
    }
}

/*
[1] Microsoft Docs. "Entity Framework Core Overview." https://learn.microsoft.com/en-us/ef/core/
[2] Microsoft Docs. "DbContextOptionsBuilder Class." https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontextoptionsbuilder
*/