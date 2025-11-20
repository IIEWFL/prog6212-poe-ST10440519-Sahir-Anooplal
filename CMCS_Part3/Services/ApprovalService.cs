using CMCS_Part3.Models;
using Microsoft.EntityFrameworkCore;

namespace CMCS_Part3.Services
{
    public class ApprovalService : IApprovalService
    {
        private readonly CMCSDbContext _context;
        private readonly ILogger<ApprovalService> _logger;

        public ApprovalService(CMCSDbContext context, ILogger<ApprovalService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ApprovalValidationResult> ValidateClaimAsync(Claim claim)
        {
            var result = new ApprovalValidationResult();

            // Load claim with lecturer data if not already loaded
            if (claim.Lecturer == null)
            {
                claim = await _context.Claims
                    .Include(c => c.Lecturer)
                    .FirstOrDefaultAsync(c => c.Id == claim.Id) ?? claim;
            }

            // Apply all approval criteria
            var criteria = GetApprovalCriteria();
            foreach (var criterion in criteria.Where(c => c.IsEnabled))
            {
                bool isValid;

                // Handle async validators separately
                if (criterion.RequiresAsyncCheck)
                {
                    isValid = await CheckAsyncCriterion(criterion, claim);
                }
                else
                {
                    isValid = criterion.Validator(claim);
                }

                if (!isValid)
                {
                    result.ValidationErrors.Add(criterion.ErrorMessage);
                }
            }

            result.IsValid = !result.ValidationErrors.Any();

            // Determine if claim can be auto-approved
            result.CanAutoApprove = CanAutoApproveClaim(claim);
            result.Recommendation = GetApprovalRecommendation(claim, result);

            return result;
        }

        public async Task<bool> AutoApproveClaimAsync(Claim claim, string approvedBy)
        {
            try
            {
                var validationResult = await ValidateClaimAsync(claim);

                if (validationResult.CanAutoApprove)
                {
                    claim.Status = ClaimStatus.Approved;
                    claim.ApprovalDate = DateTime.Now;
                    claim.ApprovedBy = $"Auto-Approved by {approvedBy}";

                    _context.Claims.Update(claim);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Claim {ClaimId} auto-approved by {ApprovedBy}", claim.Id, approvedBy);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-approving claim {ClaimId}", claim.Id);
                return false;
            }
        }

        public async Task<ApprovalWorkflowResult> ProcessApprovalWorkflowAsync(int claimId, string approvedBy, bool isApproved, string? rejectionReason = null)
        {
            var result = new ApprovalWorkflowResult();

            try
            {
                var claim = await _context.Claims
                    .Include(c => c.Lecturer)
                    .FirstOrDefaultAsync(c => c.Id == claimId);

                if (claim == null)
                {
                    result.Success = false;
                    result.Message = "Claim not found";
                    return result;
                }

                // Validate claim before processing
                var validationResult = await ValidateClaimAsync(claim);

                if (isApproved)
                {
                    if (!validationResult.IsValid && validationResult.ValidationErrors.Any())
                    {
                        result.Success = false;
                        result.Message = $"Cannot approve claim with validation errors: {string.Join(", ", validationResult.ValidationErrors)}";
                        return result;
                    }

                    claim.Status = ClaimStatus.Approved;
                    claim.ApprovalDate = DateTime.Now;
                    claim.ApprovedBy = approvedBy;
                    claim.RejectionReason = null;

                    result.Message = validationResult.CanAutoApprove
                        ? "Claim auto-approved successfully"
                        : "Claim manually approved successfully";
                }
                else
                {
                    claim.Status = ClaimStatus.Rejected;
                    claim.ApprovalDate = DateTime.Now;
                    claim.ApprovedBy = approvedBy;
                    claim.RejectionReason = rejectionReason ?? "No reason provided";

                    result.Message = "Claim rejected successfully";
                }

                _context.Claims.Update(claim);
                await _context.SaveChangesAsync();

                result.Success = true;
                result.NewStatus = claim.Status;
                result.UpdatedClaim = claim;

                _logger.LogInformation("Claim {ClaimId} {Action} by {ApprovedBy}",
                    claimId, isApproved ? "approved" : "rejected", approvedBy);
            }
            catch (Exception)
            {
                result.Success = false;
                result.Message = "An error occurred while processing the approval";
                _logger.LogError("Error processing approval workflow for claim {ClaimId}", claimId);
            }

            return result;
        }

        public List<ApprovalCriteria> GetApprovalCriteria()
        {
            return new List<ApprovalCriteria>
            {
                new ApprovalCriteria
                {
                    Name = "HoursLimit",
                    Description = "Hours worked must be between 1 and 200",
                    Validator = claim => claim.HoursWorked >= 1 && claim.HoursWorked <= 200,
                    ErrorMessage = "Hours worked must be between 1 and 200",
                    RequiresAsyncCheck = false
                },
                new ApprovalCriteria
                {
                    Name = "RateLimit",
                    Description = "Hourly rate must be between R100 and R1000",
                    Validator = claim => claim.HourlyRate >= 100 && claim.HourlyRate <= 1000,
                    ErrorMessage = "Hourly rate must be between R100 and R1000",
                    RequiresAsyncCheck = false
                },
                new ApprovalCriteria
                {
                    Name = "TotalAmountLimit",
                    Description = "Total amount must not exceed R50,000",
                    Validator = claim => claim.TotalAmount <= 50000,
                    ErrorMessage = "Total amount exceeds maximum limit of R50,000",
                    RequiresAsyncCheck = false
                },
                new ApprovalCriteria
                {
                    Name = "DuplicateMonthPrevention",
                    Description = "No duplicate claims for the same month",
                    ErrorMessage = "A claim already exists for this month",
                    RequiresAsyncCheck = true
                },
                new ApprovalCriteria
                {
                    Name = "SupportingDocuments",
                    Description = "Claims over R10,000 require supporting documents",
                    ErrorMessage = "Claims over R10,000 require supporting documents",
                    RequiresAsyncCheck = true
                },
                new ApprovalCriteria
                {
                    Name = "LecturerActive",
                    Description = "Lecturer must be active",
                    ErrorMessage = "Lecturer account is not active",
                    RequiresAsyncCheck = true
                }
            };
        }

        public async Task<List<Claim>> GetClaimsForAutoApprovalAsync()
        {
            var claims = await _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.SupportingDocuments)
                .Where(c => c.Status == ClaimStatus.Pending)
                .ToListAsync();

            var autoApproveClaims = new List<Claim>();

            foreach (var claim in claims)
            {
                if (CanAutoApproveClaim(claim))
                {
                    autoApproveClaims.Add(claim);
                }
            }

            return autoApproveClaims;
        }

        private bool CanAutoApproveClaim(Claim claim)
        {
            // Auto-approve criteria:
            // Total amount less than R5,000
            // Has supporting documents
            // Hours within normal range (1-80)
            // Rate within normal range (100-500)

            return claim.TotalAmount <= 5000 &&
                   claim.SupportingDocuments.Any() &&
                   claim.HoursWorked >= 1 && claim.HoursWorked <= 80 &&
                   claim.HourlyRate >= 100 && claim.HourlyRate <= 500;
        }

        private string GetApprovalRecommendation(Claim claim, ApprovalValidationResult validationResult)
        {
            if (!validationResult.IsValid)
                return "REJECT - Validation errors found";

            if (validationResult.CanAutoApprove)
                return "AUTO-APPROVE - Meets all auto-approval criteria";

            if (claim.TotalAmount > 20000)
                return "MANAGER REVIEW - High value claim";

            if (!claim.SupportingDocuments.Any())
                return "COORDINATOR REVIEW - No supporting documents";

            if (claim.HoursWorked > 100)
                return "COORDINATOR REVIEW - High hours worked";

            return "COORDINATOR REVIEW - Standard claim";
        }

        private async Task<bool> CheckAsyncCriterion(ApprovalCriteria criterion, Claim claim)
        {
            return criterion.Name switch
            {
                "DuplicateMonthPrevention" => await CheckDuplicateMonthAsync(claim),
                "SupportingDocuments" => await CheckSupportingDocumentsAsync(claim),
                "LecturerActive" => await CheckLecturerActiveAsync(claim),
                _ => true
            };
        }

        private async Task<bool> CheckDuplicateMonthAsync(Claim claim)
        {
            var existing = await _context.Claims
                .AnyAsync(c => c.LecturerId == claim.LecturerId &&
                             c.ClaimMonth == claim.ClaimMonth &&
                             c.Id != claim.Id &&
                             c.Status != ClaimStatus.Rejected);
            return !existing;
        }

        private async Task<bool> CheckSupportingDocumentsAsync(Claim claim)
        {
            if (claim.TotalAmount <= 10000) return true;

            var hasDocuments = await _context.SupportingDocuments
                .AnyAsync(d => d.ClaimId == claim.Id);
            return hasDocuments;
        }

        private async Task<bool> CheckLecturerActiveAsync(Claim claim)
        {
            var lecturer = await _context.Lecturers
                .FirstOrDefaultAsync(l => l.Id == claim.LecturerId);
            return lecturer?.IsActive == true;
        }
    }
}