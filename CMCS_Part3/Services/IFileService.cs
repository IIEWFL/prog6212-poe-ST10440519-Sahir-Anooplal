using CMCS_Part3.Models;

namespace CMCS_Part3.Services
{
    public interface IFileService
    {
        Task<FileUploadResult> UploadFileAsync(IFormFile file, int claimId);
        Task<bool> DeleteFileAsync(string storedFileName);
        List<string> GetSupportedFileTypes();
        long GetMaxFileSize();
        string GetFilePreviewUrl(string storedFileName);
    }

    public class FileUploadResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public SupportingDocument? Document { get; set; }
        public string? StoredFileName { get; set; }
    }
}