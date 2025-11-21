using CMCS_Part3.Data;
using CMCS_Part3.Models;
using CMCS_Part3.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CMCS_Part3.Tests.Services
{
    public class ReportServiceTests
    {
        private ApplicationDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>() //[2]
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) //[2]
                .Options;

            return new ApplicationDbContext(options); //[1]
        }

        private ILogger<ReportService> GetLogger()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole()); //[2]
            return loggerFactory.CreateLogger<ReportService>(); //[2]
        }

        [Fact]
        public async Task GetApprovedClaimsCount_NoApprovedClaims_ReturnsZero()
        {
            // Arrange
            using var context = GetDbContext(); //[1]
            var logger = GetLogger();
            var service = new ReportService(context, logger); //[1]

            // Act
            var result = await service.GetApprovedClaimsCountAsync(); //[1]

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task GetApprovedClaimsCount_WithApprovedClaims_ReturnsCount()
        {
            // Arrange
            using var context = GetDbContext(); //[1]
            var logger = GetLogger(); //[2]

            // Add approved claims
            var claims = new[]
            {
                new Claim { LecturerId = "test-1", HoursWorked = 40, HourlyRate = 200, Status = ClaimStatus.Approved },
                new Claim { LecturerId = "test-2", HoursWorked = 35, HourlyRate = 250, Status = ClaimStatus.Approved },
                new Claim { LecturerId = "test-3", HoursWorked = 30, HourlyRate = 300, Status = ClaimStatus.Pending } // Not approved
            };

            context.Claims.AddRange(claims); //[1]
            await context.SaveChangesAsync(); //[1]

            var service = new ReportService(context, logger);

            // Act
            var result = await service.GetApprovedClaimsCountAsync(); //[1]

            // Assert
            Assert.Equal(2, result); // Only 2 approved claims
        }

        [Fact]
        public async Task GetTotalMonthlyAmount_NoApprovedClaims_ReturnsZero()
        {
            // Arrange
            using var context = GetDbContext(); //[1]
            var logger = GetLogger(); //[2]
            var service = new ReportService(context, logger); //[1]

            // Act
            var result = await service.GetTotalMonthlyAmountAsync(); //[1]

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task GetTotalMonthlyAmount_WithApprovedClaims_ReturnsCorrectTotal()
        {
            // Arrange
            using var context = GetDbContext(); //[1]
            var logger = GetLogger(); //[2]

            // Add approved claims for current month
            var claims = new[]
            {
                new Claim {
                    LecturerId = "test-1",
                    HoursWorked = 40,
                    HourlyRate = 200,
                    Status = ClaimStatus.Approved,
                    SubmittedDate = DateTime.UtcNow //[1]
                },
                new Claim {
                    LecturerId = "test-2",
                    HoursWorked = 35,
                    HourlyRate = 250,
                    Status = ClaimStatus.Approved,
                    SubmittedDate = DateTime.UtcNow //[1]
                }
            };

            context.Claims.AddRange(claims);
            await context.SaveChangesAsync();

            var service = new ReportService(context, logger); //[1]

            // Act
            var result = await service.GetTotalMonthlyAmountAsync();

            // Assert
            var expectedTotal = (40 * 200) + (35 * 250); // 8000 + 8750 = 16750
            Assert.Equal(expectedTotal, result);
        }

        [Fact]
        public async Task GetAllLecturers_NoLecturers_ReturnsEmptyList()
        {
            // Arrange
            using var context = GetDbContext(); //[1]
            var logger = GetLogger(); //[2]
            var service = new ReportService(context, logger);

            // Act
            var result = await service.GetAllLecturersAsync(); //[1]

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetMonthlyReport_NoClaims_ReturnsEmptyList()
        {
            // Arrange
            using var context = GetDbContext(); //[1]
            var logger = GetLogger(); //[2]
            var service = new ReportService(context, logger);

            // Act
            var result = await service.GetMonthlyReportAsync(1, 2024); //[1]

            // Assert
            Assert.Empty(result);
        }
    }
}

/*
[1] Microsoft Docs. "Entity Framework Core Overview." https://learn.microsoft.com/en-us/ef/core/
[2] Microsoft Docs. "ILogger Interface." https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger
*/