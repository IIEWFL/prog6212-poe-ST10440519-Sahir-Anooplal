using System.ComponentModel.DataAnnotations;

namespace CMCS_Part3.Models
{
    public class Claim
    {
        public int Id { get; set; }

        [Display(Name = "Submission Date")]
        public DateTime SubmissionDate { get; set; } = DateTime.Now;

        [Display(Name = "Month")]
        [Required(ErrorMessage = "Please select a month.")]
        public string ClaimMonth { get; set; } = DateTime.Now.ToString("yyyy-MM");

        [Display(Name = "Hours Worked")]
        [Range(1, 200, ErrorMessage = "Hours worked must be between 1 and 200.")]
        public double HoursWorked { get; set; }

        [Display(Name = "Hourly Rate")]
        [Range(100, 1000, ErrorMessage = "Hourly rate must be between R100 and R1000.")]
        [DataType(DataType.Currency)]
        public decimal HourlyRate { get; set; }

        [Display(Name = "Additional Notes")]
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string? Notes { get; set; }

        [Display(Name = "Status")]
        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

        [Display(Name = "Approval Date")]
        public DateTime? ApprovalDate { get; set; }

        [Display(Name = "Approved By")]
        public string? ApprovedBy { get; set; }

        [Display(Name = "Rejection Reason")]
        public string? RejectionReason { get; set; }

        // Foreign Key
        public int LecturerId { get; set; }

        // Navigation properties
        public Lecturer? Lecturer { get; set; }
        public List<SupportingDocument> SupportingDocuments { get; set; } = new List<SupportingDocument>();

        // Automated calculated properties
        [Display(Name = "Total Amount")]
        public decimal TotalAmount => (decimal)HoursWorked * HourlyRate;

        [Display(Name = "Days Pending")]
        public int DaysPending => (DateTime.Now - SubmissionDate).Days;

        [Display(Name = "IsOverdue")]
        public bool IsOverdue => DaysPending > 30 && Status == ClaimStatus.Pending;
    }

    public enum ClaimStatus
    {
        Pending,
        Approved,
        Rejected
    }
}