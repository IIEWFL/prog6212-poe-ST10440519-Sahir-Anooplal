using Microsoft.EntityFrameworkCore;
using CMCS_Part3.Data;
using CMCS_Part3.Models;
using Microsoft.Extensions.Logging;

namespace CMCS_Part3.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context; //[2]
        private readonly ILogger<ReportService> _logger; //[1]

        public ReportService(ApplicationDbContext context, ILogger<ReportService> logger)
        {
            _context = context; //[2]
            _logger = logger; //[1]
        }

        public async Task<int> GetApprovedClaimsCountAsync()
        {
            return await _context.Claims
                .Where(c => c.Status == ClaimStatus.Approved) //[2]
                .CountAsync(); //[1]
        }

        public async Task<decimal> GetTotalMonthlyAmountAsync()
        {
            var startDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1); //[1]
            var endDate = startDate.AddMonths(1).AddDays(-1); //[1]

            var claims = await _context.Claims
                .Where(c => c.Status == ClaimStatus.Approved && c.SubmittedDate >= startDate && c.SubmittedDate <= endDate)
                .ToListAsync(); //[1]

            return claims.Sum(c => c.HoursWorked * c.HourlyRate);
        }

        public async Task<int> GetPendingPaymentsCountAsync()
        {
            return await _context.Claims
                .Where(c => c.Status == ClaimStatus.Approved) //[2]
                .CountAsync(); //[1]
        }

        public async Task<List<Claim>> GetMonthlyReportAsync(int month, int year)
        {
            var startDate = new DateTime(year, month, 1); //[1]
            var endDate = startDate.AddMonths(1).AddDays(-1); //[1]

            return await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.SubmittedDate >= startDate && c.SubmittedDate <= endDate)
                .OrderByDescending(c => c.SubmittedDate)
                .ToListAsync();
        }

        public async Task<List<ApplicationUser>> GetAllLecturersAsync()
        {
            return await _context.Users
                .Where(u => u.Role == "Lecturer")
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalAmountForPeriodAsync(DateTime startDate, DateTime endDate)
        {
            var claims = await _context.Claims
                .Where(c => c.Status == ClaimStatus.Approved && c.SubmittedDate >= startDate && c.SubmittedDate <= endDate)
                .ToListAsync(); 

            return claims.Sum(c => c.HoursWorked * c.HourlyRate); //[2]
        }

        public async Task<List<Claim>> GetClaimsForApprovalAsync()
        {
            return await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Pending)
                .OrderBy(c => c.SubmittedDate)
                .ToListAsync();
        }
    }
}

/*
[1] Microsoft Docs. "Logging and Dependency Injection in ASP.NET Core." https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging
[2] Microsoft Docs. "Entity Framework Core Fundamentals." https://learn.microsoft.com/en-us/ef/core/
*/