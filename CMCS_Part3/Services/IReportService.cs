using CMCS_Part3.Models;

namespace CMCS_Part3.Services
{
    public interface IReportService
    {
        Task<int> GetApprovedClaimsCountAsync();
        Task<decimal> GetTotalMonthlyAmountAsync();
        Task<int> GetPendingPaymentsCountAsync();
        Task<List<Claim>> GetMonthlyReportAsync(int month, int year);
        Task<List<ApplicationUser>> GetAllLecturersAsync();
        Task<decimal> GetTotalAmountForPeriodAsync(DateTime startDate, DateTime endDate);
        Task<List<Claim>> GetClaimsForApprovalAsync();
    }
}