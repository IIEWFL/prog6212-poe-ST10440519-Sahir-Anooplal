using Microsoft.EntityFrameworkCore;
using CMCS_Part3.Data;
using CMCS_Part3.Models;

namespace CMCS_Part3.Services
{
    public class ClaimService : IClaimService
    {
        private readonly ApplicationDbContext _context; //[2]
        private readonly IWebHostEnvironment _environment; //[1]

        public ClaimService(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context; //[2]
            _environment = environment; //[1]
        }

        public async Task<ServiceResult> SubmitClaimAsync(Claim claim, IFormFile? supportingDocument)
        {
            try
            {
                // Validate business rules
                if (claim.HoursWorked > 176)
                    return ServiceResult.Fail("Maximum 176 hours per month allowed");

                if (claim.HourlyRate < 100 || claim.HourlyRate > 1000)
                    return ServiceResult.Fail("Hourly rate must be between R100 and R1000");

                // Handle document upload
                if (supportingDocument != null && supportingDocument.Length > 0)
                {
                    var uploadResult = await UploadDocumentAsync(supportingDocument, claim.ClaimId); //[1]
                    if (!uploadResult.Success)
                        return ServiceResult.Fail(uploadResult.ErrorMessage);

                    claim.DocumentPath = uploadResult.FilePath; //[1]
                    claim.DocumentName = supportingDocument.FileName; //[1]
                }

                claim.Status = ClaimStatus.Pending; //[2]
                claim.SubmittedDate = DateTime.UtcNow; //[2]

                _context.Claims.Add(claim);
                await _context.SaveChangesAsync();

                return ServiceResult.Ok();
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail($"Error submitting claim: {ex.Message}"); //[2]
            }
        }

        public async Task<List<Claim>> GetPendingClaimsAsync()
        {
            return await _context.Claims //[2]
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Pending)
                .OrderBy(c => c.SubmittedDate)
                .ToListAsync(); //[1]
        }

        public async Task<List<Claim>> GetClaimsByLecturerAsync(string lecturerId)
        {
            return await _context.Claims //[2]
                .Where(c => c.LecturerId == lecturerId)
                .OrderByDescending(c => c.SubmittedDate)
                .ToListAsync(); //[1]
        }

        public async Task<List<Claim>> GetAllClaimsAsync()
        {
            return await _context.Claims //[2]
                .Include(c => c.Lecturer)
                .OrderByDescending(c => c.SubmittedDate)
                .ToListAsync(); //[1]
        }

        public async Task<ServiceResult> ApproveClaimAsync(int claimId, string approvedBy)
        {
            try
            {
                var claim = await _context.Claims.FindAsync(claimId); //[2]
                if (claim == null)
                    return ServiceResult.Fail("Claim not found"); //[2]

                claim.Status = ClaimStatus.Approved;
                claim.ProcessedBy = approvedBy;
                claim.ProcessedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return ServiceResult.Ok(); //[2]
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail($"Error approving claim: {ex.Message}"); //[2]
            }
        }

        public async Task<ServiceResult> RejectClaimAsync(int claimId, string rejectedBy, string reason)
        {
            try
            {
                var claim = await _context.Claims.FindAsync(claimId); //[2]
                if (claim == null)
                    return ServiceResult.Fail("Claim not found"); //[2]

                claim.Status = ClaimStatus.Rejected;
                claim.ProcessedBy = rejectedBy;
                claim.ProcessedDate = DateTime.UtcNow;
                claim.RejectionReason = reason;

                await _context.SaveChangesAsync();
                return ServiceResult.Ok(); //[2]
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail($"Error rejecting claim: {ex.Message}"); //[2]
            }
        }

        public async Task<Claim?> GetClaimByIdAsync(int claimId)
        {
            return await _context.Claims
                .Include(c => c.Lecturer) //[2]
                .FirstOrDefaultAsync(c => c.ClaimId == claimId); //[1]
        }

        public async Task<ServiceResult> UpdateClaimStatusAsync(int claimId, ClaimStatus status, string processedBy)
        {
            try
            {
                var claim = await _context.Claims.FindAsync(claimId); //[2]
                if (claim == null)
                    return ServiceResult.Fail("Claim not found"); //[2]

                claim.Status = status;
                claim.ProcessedBy = processedBy;
                claim.ProcessedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync(); //[2]
                return ServiceResult.Ok(); //[2]
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail($"Error updating claim status: {ex.Message}"); //[2]
            }
        }

        private async Task<ServiceResult> UploadDocumentAsync(IFormFile file, int claimId)
        {
            try
            {
                var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx", ".jpg", ".png" }; //[1]
                var maxFileSize = 5 * 1024 * 1024; // 5MB

                if (file.Length > maxFileSize)
                    return ServiceResult.Fail("File size must be less than 5MB");

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension)) //[1]
                    return ServiceResult.Fail("Only PDF, DOCX, XLSX, JPG, and PNG files are allowed");

                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) //[1]
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{claimId}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}"; //[1]
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create)) //[1]
                {
                    await file.CopyToAsync(stream);
                }

                return ServiceResult.OkWithFile($"/uploads/{fileName}"); //[1]
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail($"Error uploading document: {ex.Message}"); //[1]
            }
        }
    }
}

/*
[1] Microsoft Docs. "Upload files in ASP.NET Core." https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads
[2] Microsoft Docs. "Entity Framework Core Fundamentals." https://learn.microsoft.com/en-us/ef/core/
*/