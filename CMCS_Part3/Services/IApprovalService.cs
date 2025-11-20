using CMCS_Part3.Models;

namespace CMCS_Part3.Services
{
    public interface IApprovalService
    {
        Task<ApprovalValidationResult> ValidateClaimAsync(Claim claim);
        Task<bool> AutoApproveClaimAsync(Claim claim, string approvedBy);
        Task<ApprovalWorkflowResult> ProcessApprovalWorkflowAsync(int claimId, string approvedBy, bool isApproved, string? rejectionReason = null);
        List<ApprovalCriteria> GetApprovalCriteria();
        Task<List<Claim>> GetClaimsForAutoApprovalAsync();
    }

    public class ApprovalValidationResult
    {
        public bool IsValid { get; set; }
        public bool CanAutoApprove { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public List<string> ApprovalWarnings { get; set; } = new List<string>();
        public string Recommendation { get; set; } = string.Empty;
    }

    public class ApprovalWorkflowResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public ClaimStatus NewStatus { get; set; }
        public Claim? UpdatedClaim { get; set; }
    }

    public class ApprovalCriteria
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public Func<Claim, bool> Validator { get; set; } = _ => true;
        public string ErrorMessage { get; set; } = string.Empty;
        public bool RequiresAsyncCheck { get; set; } = false;
    }
}