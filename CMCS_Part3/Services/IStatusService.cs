using CMCS_Part3.Models;

namespace CMCS_Part3.Services
{
    public interface IStatusService
    {
        Task<StatusUpdateResult> UpdateClaimStatusAsync(int claimId, ClaimStatus newStatus, string updatedBy, string? notes = null);
        Task<List<StatusHistory>> GetStatusHistoryAsync(int claimId);
        Task<StatusOverview> GetStatusOverviewAsync();
        Task<List<Claim>> GetOverdueClaimsAsync();
        Task SendStatusNotificationAsync(Claim claim, string message);
        Task<ClaimStatus> GetClaimStatusAsync(int claimId);
    }

    public class StatusUpdateResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Claim? UpdatedClaim { get; set; }
        public ClaimStatus PreviousStatus { get; set; }
        public ClaimStatus NewStatus { get; set; }
    }

    public class StatusHistory
    {
        public int ClaimId { get; set; }
        public ClaimStatus Status { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class StatusOverview
    {
        public int TotalClaims { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int OverdueCount { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal ApprovedAmount { get; set; }
        public List<StatusTrend> RecentTrends { get; set; } = new List<StatusTrend>();
    }

    public class StatusTrend
    {
        public DateTime Date { get; set; }
        public int Pending { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
    }
}