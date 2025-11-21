using System.ComponentModel.DataAnnotations;

namespace CMCS_Part3.ViewModels
{
    public class ClaimViewModel
    {
        public int ClaimId { get; set; }

        [Required]
        [Display(Name = "Hours Worked")]
        [Range(1, 176, ErrorMessage = "Hours worked must be between 1 and 176")]
        public decimal HoursWorked { get; set; }

        [Required]
        [Display(Name = "Hourly Rate (R)")]
        [Range(100, 1000, ErrorMessage = "Hourly rate must be between R100 and R1000")]
        public decimal HourlyRate { get; set; }

        [Display(Name = "Total Amount")]
        public decimal TotalAmount => HoursWorked * HourlyRate;

        [Display(Name = "Additional Notes")]
        [StringLength(500)]
        public string? AdditionalNotes { get; set; }

        [Display(Name = "Supporting Document")]
        public IFormFile? SupportingDocument { get; set; }
    }

    public class ClaimStatusViewModel
    {
        public int ClaimId { get; set; }
        public string LecturerName { get; set; } = string.Empty;
        public decimal HoursWorked { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime SubmittedDate { get; set; }
        public string? DocumentName { get; set; }
    }
}