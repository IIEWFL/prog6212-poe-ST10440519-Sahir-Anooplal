using System.ComponentModel.DataAnnotations;

namespace CMCS_Part3.Models
{
    public class Report
    {
        public int Id { get; set; }

        [Display(Name = "Report Name")]
        [Required(ErrorMessage = "Report name is required.")]
        public string ReportName { get; set; } = string.Empty;

        [Display(Name = "Report Type")]
        public ReportType Type { get; set; }

        [Display(Name = "Generated Date")]
        public DateTime GeneratedDate { get; set; } = DateTime.Now;

        [Display(Name = "Date From")]
        [DataType(DataType.Date)]
        public DateTime DateFrom { get; set; } = DateTime.Now.AddMonths(-1);

        [Display(Name = "Date To")]
        [DataType(DataType.Date)]
        public DateTime DateTo { get; set; } = DateTime.Now;

        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Total Claims")]
        public int TotalClaims { get; set; }

        [Display(Name = "File Path")]
        public string? FilePath { get; set; }
    }

    public enum ReportType
    {
        MonthlyClaims,
        LecturerPerformance,
        DepartmentSummary,
        PaymentSummary
    }
}