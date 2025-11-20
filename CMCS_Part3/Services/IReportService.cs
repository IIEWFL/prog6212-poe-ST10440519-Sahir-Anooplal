using CMCS_Part3.Models;

namespace CMCS_Part3.Services
{
    public interface IReportService
    {
        Task<ReportResult> GenerateMonthlyClaimsReportAsync(DateTime fromDate, DateTime toDate);
        Task<ReportResult> GenerateLecturerPerformanceReportAsync(DateTime fromDate, DateTime toDate);
        Task<ReportResult> GenerateDepartmentSummaryReportAsync(DateTime fromDate, DateTime toDate);
        Task<ReportResult> GeneratePaymentSummaryReportAsync(DateTime fromDate, DateTime toDate);
        Task<byte[]> GenerateExcelReportAsync(ReportResult reportResult);
        Task<byte[]> GeneratePdfReportAsync(ReportResult reportResult);
        Task<string> SaveReportAsync(ReportResult reportResult, string reportName, ReportType reportType);
        Task<List<Report>> GetRecentReportsAsync(int count = 10);
    }

    public class ReportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ReportTitle { get; set; } = string.Empty;
        public ReportType ReportType { get; set; }
        public DateTime GeneratedDate { get; set; } = DateTime.Now;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<ReportDataItem> Data { get; set; } = new List<ReportDataItem>();
        public ReportSummary Summary { get; set; } = new ReportSummary();
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
    }

    public class ReportDataItem
    {
        public string Category { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
        public int ClaimCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
        public DateTime Period { get; set; }
        public object? Entity { get; set; }
    }

    public class ReportSummary
    {
        public int TotalClaims { get; set; }
        public decimal TotalAmount { get; set; }
        public int ApprovedClaims { get; set; }
        public int PendingClaims { get; set; }
        public int RejectedClaims { get; set; }
        public decimal AverageClaimAmount { get; set; }
        public int UniqueLecturers { get; set; }
        public int DepartmentsCount { get; set; }
    }
}