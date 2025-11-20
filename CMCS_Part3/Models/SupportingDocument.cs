using System.ComponentModel.DataAnnotations;

namespace CMCS_Part3.Models
{
    public class SupportingDocument
    {
        public int Id { get; set; }

        [Display(Name = "File Name")]
        public string FileName { get; set; } = string.Empty;

        [Display(Name = "Stored File Name")]
        public string StoredFileName { get; set; } = string.Empty;

        [Display(Name = "File Size")]
        public long FileSize { get; set; }

        [Display(Name = "File Type")]
        public string FileType { get; set; } = string.Empty;

        [Display(Name = "Upload Date")]
        public DateTime UploadDate { get; set; } = DateTime.Now;

        [Display(Name = "Description")]
        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters.")]
        public string? Description { get; set; }

        // Foreign Key
        public int ClaimId { get; set; }

        // Navigation property
        public Claim? Claim { get; set; }
    }
}