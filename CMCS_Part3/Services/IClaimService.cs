using CMCS_Part3.Models;

namespace CMCS_Part3.Services
{
    public interface IClaimService
    {
        Task<ServiceResult> SubmitClaimAsync(Claim claim, IFormFile? supportingDocument);
        Task<List<Claim>> GetPendingClaimsAsync();
        Task<List<Claim>> GetClaimsByLecturerAsync(string lecturerId);
        Task<List<Claim>> GetAllClaimsAsync();
        Task<ServiceResult> ApproveClaimAsync(int claimId, string approvedBy);
        Task<ServiceResult> RejectClaimAsync(int claimId, string rejectedBy, string reason);
        Task<Claim?> GetClaimByIdAsync(int claimId);
        Task<ServiceResult> UpdateClaimStatusAsync(int claimId, ClaimStatus status, string processedBy);
    }

    public class ServiceResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;

        public static ServiceResult Ok() => new ServiceResult { Success = true };
        public static ServiceResult Fail(string message) => new ServiceResult { Success = false, ErrorMessage = message };
        public static ServiceResult OkWithFile(string filePath) => new ServiceResult { Success = true, FilePath = filePath };
    }
}