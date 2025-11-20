using CMCS_Part3.Models;
using Microsoft.EntityFrameworkCore;

namespace CMCS_Part3.Services
{
    public class StatusService : IStatusService
    {
        private readonly CMCSDbContext _context;
        private readonly ILogger<StatusService> _logger;

        public StatusService(CMCSDbContext context, ILogger<StatusService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<StatusUpdateResult> UpdateClaimStatusAsync(int claimId, ClaimStatus newStatus, string updatedBy, string? notes = null)
        {
            var result = new StatusUpdateResult();

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

                result.PreviousStatus = claim.Status;
                result.NewStatus = newStatus;

                // Update claim status
                claim.Status = newStatus;

                // Set approval/rejection details
                if (newStatus == ClaimStatus.Approved)
                {
                    claim.ApprovalDate = DateTime.Now;
                    claim.ApprovedBy = updatedBy;
                    claim.RejectionReason = null;
                }
                else if (newStatus == ClaimStatus.Rejected)
                {
                    claim.ApprovalDate = DateTime.Now;
                    claim.ApprovedBy = updatedBy;
                    claim.RejectionReason = notes ?? "No reason provided";
                }

                _context.Claims.Update(claim);

                // Log status history
                await LogStatusHistory(claimId, newStatus, updatedBy, notes);

                await _context.SaveChangesAsync();

                result.Success = true;
                result.Message = $"Claim status updated from {result.PreviousStatus} to {newStatus}";
                result.UpdatedClaim = claim;

                // Send notification
                await SendStatusNotificationAsync(claim, $"Claim status updated to {newStatus}");

                _logger.LogInformation("Claim {ClaimId} status updated from {PreviousStatus} to {NewStatus} by {User}",
                    claimId, result.PreviousStatus, newStatus, updatedBy);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = "Error updating claim status";
                _logger.LogError(ex, "Error updating status for claim {ClaimId}", claimId);
            }

            return result;
        }

        public async Task<List<StatusHistory>> GetStatusHistoryAsync(int claimId)
        {
            var claim = await _context.Claims
                .Include(c => c.Lecturer)
                .FirstOrDefaultAsync(c => c.Id == claimId);

            if (claim == null)
                return new List<StatusHistory>();

            var history = new List<StatusHistory>
            {
                new StatusHistory
                {
                    ClaimId = claimId,
                    Status = ClaimStatus.Pending,
                    UpdatedBy = "System",
                    UpdatedAt = claim.SubmissionDate,
                    Notes = "Claim submitted"
                }
            };

            if (claim.ApprovalDate.HasValue)
            {
                history.Add(new StatusHistory
                {
                    ClaimId = claimId,
                    Status = claim.Status,
                    UpdatedBy = claim.ApprovedBy ?? "Unknown",
                    UpdatedAt = claim.ApprovalDate.Value,
                    Notes = claim.Status == ClaimStatus.Approved ? "Claim approved" : $"Claim rejected: {claim.RejectionReason}"
                });
            }

            return history.OrderBy(h => h.UpdatedAt).ToList();
        }

        public async Task<StatusOverview> GetStatusOverviewAsync()
        {
            var claims = await _context.Claims.ToListAsync();
            var overview = new StatusOverview
            {
                TotalClaims = claims.Count,
                PendingCount = claims.Count(c => c.Status == ClaimStatus.Pending),
                ApprovedCount = claims.Count(c => c.Status == ClaimStatus.Approved),
                RejectedCount = claims.Count(c => c.Status == ClaimStatus.Rejected),
                OverdueCount = claims.Count(c => c.Status == ClaimStatus.Pending && (DateTime.Now - c.SubmissionDate).Days > 30),
                PendingAmount = claims.Where(c => c.Status == ClaimStatus.Pending).Sum(c => c.TotalAmount),
                ApprovedAmount = claims.Where(c => c.Status == ClaimStatus.Approved).Sum(c => c.TotalAmount)
            };

            // Generate recent trends (last 7 days)
            overview.RecentTrends = await GetRecentStatusTrends();

            return overview;
        }

        public async Task<List<Claim>> GetOverdueClaimsAsync()
        {
            return await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Pending && (DateTime.Now - c.SubmissionDate).Days > 30)
                .OrderBy(c => c.SubmissionDate)
                .ToListAsync();
        }

        public Task SendStatusNotificationAsync(Claim claim, string message)
        {
            _logger.LogInformation("Status notification for claim {ClaimId}: {Message}", claim.Id, message);
            return Task.CompletedTask;
        }

        public async Task<ClaimStatus> GetClaimStatusAsync(int claimId)
        {
            var claim = await _context.Claims
                .FirstOrDefaultAsync(c => c.Id == claimId);

            return claim?.Status ?? ClaimStatus.Pending;
        }

        private async Task<List<StatusTrend>> GetRecentStatusTrends()
        {
            var trends = new List<StatusTrend>();
            var today = DateTime.Today;

            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var dayStart = date.Date;
                var dayEnd = date.Date.AddDays(1).AddTicks(-1);

                var daysClaims = await _context.Claims
                    .Where(c => c.SubmissionDate >= dayStart && c.SubmissionDate <= dayEnd)
                    .ToListAsync();

                trends.Add(new StatusTrend
                {
                    Date = date,
                    Pending = daysClaims.Count(c => c.Status == ClaimStatus.Pending),
                    Approved = daysClaims.Count(c => c.Status == ClaimStatus.Approved),
                    Rejected = daysClaims.Count(c => c.Status == ClaimStatus.Rejected)
                });
            }

            return trends;
        }

        private Task LogStatusHistory(int claimId, ClaimStatus status, string updatedBy, string? notes)
        {
            _logger.LogInformation("Status history - Claim: {ClaimId}, Status: {Status}, By: {User}, Notes: {Notes}",
                claimId, status, updatedBy, notes ?? "None");
            return Task.CompletedTask;
        }
    }
}