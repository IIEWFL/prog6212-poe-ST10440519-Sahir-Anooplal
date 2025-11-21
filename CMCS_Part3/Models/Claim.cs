using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMCS_Part3.Models
{
    public class Claim
    {
        [Key]
        public int ClaimId { get; set; }

        [Required]
        public string LecturerId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Hours Worked")]
        [Range(1, 176, ErrorMessage = "Hours worked must be between 1 and 176")]
        public decimal HoursWorked { get; set; }

        [Required]
        [Display(Name = "Hourly Rate (R)")]
        [Range(100, 1000, ErrorMessage = "Hourly rate must be between R100 and R1000")]
        public decimal HourlyRate { get; set; }

        [NotMapped]
        [Display(Name = "Total Amount")]
        public decimal TotalAmount => HoursWorked * HourlyRate;

        [Display(Name = "Additional Notes")]
        [StringLength(500)]
        public string? AdditionalNotes { get; set; }

        [Display(Name = "Supporting Document")]
        public string? DocumentPath { get; set; }

        [Display(Name = "Document Name")]
        public string? DocumentName { get; set; }

        [Required]
        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

        [Display(Name = "Submitted Date")]
        public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Processed Date")]
        public DateTime? ProcessedDate { get; set; }

        [Display(Name = "Processed By")]
        public string? ProcessedBy { get; set; }

        [Display(Name = "Rejection Reason")]
        [StringLength(500)]
        public string? RejectionReason { get; set; }

        // Navigation properties
        public virtual ApplicationUser? Lecturer { get; set; }
    }

    public enum ClaimStatus
    {
        Pending,
        Approved,
        Rejected,
        Paid
    }
}