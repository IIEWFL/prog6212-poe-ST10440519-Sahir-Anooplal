using CMCS_Part3.Models;
using Microsoft.EntityFrameworkCore;

namespace CMCS_Part3.Services
{
    public class FileService : IFileService
    {
        private readonly CMCSDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileService> _logger;

        public FileService(CMCSDbContext context, IWebHostEnvironment environment, ILogger<FileService> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        public async Task<FileUploadResult> UploadFileAsync(IFormFile file, int claimId)
        {
            var result = new FileUploadResult();

            try
            {
                // Validate file
                var validationResult = ValidateFile(file);
                if (!validationResult.Success)
                {
                    result.Success = false;
                    result.Message = validationResult.Message;
                    return result;
                }

                // Generate unique filename
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var storedFileName = $"{Guid.NewGuid()}{fileExtension}";
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");

                // Ensure uploads directory exists
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var filePath = Path.Combine(uploadsFolder, storedFileName);

                // Save file to disk
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Create document record
                var document = new SupportingDocument
                {
                    FileName = file.FileName,
                    StoredFileName = storedFileName,
                    FileSize = file.Length,
                    FileType = fileExtension,
                    Description = $"Uploaded for claim #{claimId}",
                    ClaimId = claimId,
                    UploadDate = DateTime.Now
                };

                _context.SupportingDocuments.Add(document);
                await _context.SaveChangesAsync();

                result.Success = true;
                result.Message = "File uploaded successfully";
                result.Document = document;
                result.StoredFileName = storedFileName;

                _logger.LogInformation("File uploaded successfully: {FileName} for claim {ClaimId}", file.FileName, claimId);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = "An error occurred while uploading the file";
                _logger.LogError(ex, "Error uploading file {FileName} for claim {ClaimId}", file.FileName, claimId);
            }

            return result;
        }

        public async Task<bool> DeleteFileAsync(string storedFileName)
        {
            try
            {
                var filePath = Path.Combine(_environment.WebRootPath, "uploads", storedFileName);

                // Delete from file system
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // Delete from database
                var document = await _context.SupportingDocuments
                    .FirstOrDefaultAsync(d => d.StoredFileName == storedFileName);

                if (document != null)
                {
                    _context.SupportingDocuments.Remove(document);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("File deleted successfully: {FileName}", storedFileName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FileName}", storedFileName);
                return false;
            }
        }

        public List<string> GetSupportedFileTypes()
        {
            return new List<string> { ".pdf", ".docx", ".xlsx" };
        }

        public long GetMaxFileSize()
        {
            return 5 * 1024 * 1024; // 5MB
        }

        public string GetFilePreviewUrl(string storedFileName)
        {
            return $"/uploads/{storedFileName}";
        }

        private FileUploadResult ValidateFile(IFormFile file)
        {
            var result = new FileUploadResult();

            // Check if file is provided
            if (file == null || file.Length == 0)
            {
                result.Success = false;
                result.Message = "No file provided";
                return result;
            }

            // Check file size
            if (file.Length > GetMaxFileSize())
            {
                result.Success = false;
                result.Message = $"File size exceeds maximum limit of {FormatFileSize(GetMaxFileSize())}";
                return result;
            }

            // Check file type
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var supportedTypes = GetSupportedFileTypes();

            if (!supportedTypes.Contains(fileExtension))
            {
                result.Success = false;
                result.Message = $"File type not supported. Allowed types: {string.Join(", ", supportedTypes)}";
                return result;
            }

            // Check for dangerous file names
            if (file.FileName.Contains("..") || file.FileName.Contains("/") || file.FileName.Contains("\\"))
            {
                result.Success = false;
                result.Message = "Invalid file name";
                return result;
            }

            result.Success = true;
            return result;
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            while (bytes >= 1024 && order < sizes.Length - 1)
            {
                order++;
                bytes /= 1024;
            }
            return $"{bytes:0.##} {sizes[order]}";
        }
    }
}