using CMCS_Part3.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace CMCS_Part3.Services
{
    public class ReportService : IReportService
    {
        private readonly CMCSDbContext _context;
        private readonly ILogger<ReportService> _logger;

        public ReportService(CMCSDbContext context, ILogger<ReportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ReportResult> GenerateMonthlyClaimsReportAsync(DateTime fromDate, DateTime toDate)
        {
            var result = new ReportResult
            {
                ReportTitle = "Monthly Claims Report",
                ReportType = ReportType.MonthlyClaims,
                FromDate = fromDate,
                ToDate = toDate
            };

            try
            {
                var claims = await _context.Claims
                    .Include(c => c.Lecturer)
                    .Include(c => c.SupportingDocuments)
                    .Where(c => c.SubmissionDate >= fromDate && c.SubmissionDate <= toDate)
                    .ToListAsync();

                // Group by month
                var monthlyData = claims
                    .GroupBy(c => new { c.SubmissionDate.Year, c.SubmissionDate.Month })
                    .Select(g => new ReportDataItem
                    {
                        Category = "Monthly Claims",
                        SubCategory = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy"),
                        ClaimCount = g.Count(),
                        TotalAmount = g.Sum(c => c.TotalAmount),
                        AverageAmount = g.Average(c => c.TotalAmount),
                        Period = new DateTime(g.Key.Year, g.Key.Month, 1)
                    })
                    .OrderBy(d => d.Period)
                    .ToList();

                result.Data = monthlyData;
                result.Summary = CalculateSummary(claims);
                result.Success = true;
                result.Message = "Monthly claims report generated successfully";

                _logger.LogInformation("Monthly claims report generated for {FromDate} to {ToDate}", fromDate, toDate);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = "Error generating monthly claims report";
                _logger.LogError(ex, "Error generating monthly claims report");
            }

            return result;
        }

        public async Task<ReportResult> GenerateLecturerPerformanceReportAsync(DateTime fromDate, DateTime toDate)
        {
            var result = new ReportResult
            {
                ReportTitle = "Lecturer Performance Report",
                ReportType = ReportType.LecturerPerformance,
                FromDate = fromDate,
                ToDate = toDate
            };

            try
            {
                var claims = await _context.Claims
                    .Include(c => c.Lecturer)
                    .Where(c => c.SubmissionDate >= fromDate && c.SubmissionDate <= toDate && c.Status == ClaimStatus.Approved)
                    .ToListAsync();

                var lecturerData = claims
                    .GroupBy(c => new { c.LecturerId, c.Lecturer!.Name, c.Lecturer.Department })
                    .Select(g => new ReportDataItem
                    {
                        Category = "Lecturer Performance",
                        SubCategory = g.Key.Name ?? "Unknown Lecturer",
                        ClaimCount = g.Count(),
                        TotalAmount = g.Sum(c => c.TotalAmount),
                        AverageAmount = g.Average(c => c.TotalAmount),
                        Entity = new
                        {
                            LecturerId = g.Key.LecturerId,
                            LecturerName = g.Key.Name,
                            Department = g.Key.Department,
                            AverageHours = g.Average(c => c.HoursWorked),
                            AverageRate = g.Average(c => c.HourlyRate)
                        }
                    })
                    .OrderByDescending(d => d.TotalAmount)
                    .ToList();

                result.Data = lecturerData;
                result.Summary = CalculateSummary(claims);
                result.Success = true;
                result.Message = "Lecturer performance report generated successfully";

                _logger.LogInformation("Lecturer performance report generated for {FromDate} to {ToDate}", fromDate, toDate);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = "Error generating lecturer performance report";
                _logger.LogError(ex, "Error generating lecturer performance report");
            }

            return result;
        }

        public async Task<ReportResult> GenerateDepartmentSummaryReportAsync(DateTime fromDate, DateTime toDate)
        {
            var result = new ReportResult
            {
                ReportTitle = "Department Summary Report",
                ReportType = ReportType.DepartmentSummary,
                FromDate = fromDate,
                ToDate = toDate
            };

            try
            {
                var claims = await _context.Claims
                    .Include(c => c.Lecturer)
                    .Where(c => c.SubmissionDate >= fromDate && c.SubmissionDate <= toDate && c.Lecturer != null)
                    .ToListAsync();

                var departmentData = claims
                    .Where(c => !string.IsNullOrEmpty(c.Lecturer!.Department))
                    .GroupBy(c => c.Lecturer!.Department)
                    .Select(g => new ReportDataItem
                    {
                        Category = "Department Summary",
                        SubCategory = g.Key!,
                        ClaimCount = g.Count(),
                        TotalAmount = g.Sum(c => c.TotalAmount),
                        AverageAmount = g.Average(c => c.TotalAmount),
                        Entity = new
                        {
                            Department = g.Key,
                            LecturerCount = g.Select(c => c.LecturerId).Distinct().Count(),
                            AverageHours = g.Average(c => c.HoursWorked),
                            ApprovalRate = g.Count(c => c.Status == ClaimStatus.Approved) / (double)Math.Max(g.Count(), 1) * 100
                        }
                    })
                    .OrderByDescending(d => d.TotalAmount)
                    .ToList();

                result.Data = departmentData;
                result.Summary = CalculateSummary(claims);
                result.Success = true;
                result.Message = "Department summary report generated successfully";

                _logger.LogInformation("Department summary report generated for {FromDate} to {ToDate}", fromDate, toDate);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = "Error generating department summary report";
                _logger.LogError(ex, "Error generating department summary report");
            }

            return result;
        }

        public async Task<ReportResult> GeneratePaymentSummaryReportAsync(DateTime fromDate, DateTime toDate)
        {
            var result = new ReportResult
            {
                ReportTitle = "Payment Summary Report",
                ReportType = ReportType.PaymentSummary,
                FromDate = fromDate,
                ToDate = toDate
            };

            try
            {
                var claims = await _context.Claims
                    .Include(c => c.Lecturer)
                    .Where(c => c.SubmissionDate >= fromDate && c.SubmissionDate <= toDate && c.Status == ClaimStatus.Approved)
                    .ToListAsync();

                // Group by lecturer for payment processing
                var paymentData = claims
                    .GroupBy(c => new { c.LecturerId, c.Lecturer!.Name, c.Lecturer.Email })
                    .Select(g => new ReportDataItem
                    {
                        Category = "Payment Summary",
                        SubCategory = g.Key.Name ?? "Unknown Lecturer",
                        ClaimCount = g.Count(),
                        TotalAmount = g.Sum(c => c.TotalAmount),
                        AverageAmount = g.Average(c => c.TotalAmount),
                        Entity = new
                        {
                            LecturerId = g.Key.LecturerId,
                            LecturerName = g.Key.Name,
                            Email = g.Key.Email,
                            Claims = g.Select(c => new
                            {
                                ClaimId = c.Id,
                                Month = c.ClaimMonth,
                                Amount = c.TotalAmount,
                                ApprovalDate = c.ApprovalDate
                            }).ToList()
                        }
                    })
                    .OrderByDescending(d => d.TotalAmount)
                    .ToList();

                result.Data = paymentData;
                result.Summary = CalculateSummary(claims);
                result.Success = true;
                result.Message = "Payment summary report generated successfully";

                _logger.LogInformation("Payment summary report generated for {FromDate} to {ToDate}", fromDate, toDate);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = "Error generating payment summary report";
                _logger.LogError(ex, "Error generating payment summary report");
            }

            return result;
        }

        public Task<byte[]> GenerateExcelReportAsync(ReportResult reportResult)
        {
            // Excel generation
            var csvContent = GenerateCsvContent(reportResult);
            var bytes = Encoding.UTF8.GetBytes(csvContent);
            return Task.FromResult(bytes);
        }

        public Task<byte[]> GeneratePdfReportAsync(ReportResult reportResult)
        {
            // PDF generation
            var htmlContent = GenerateHtmlContent(reportResult);
            var bytes = Encoding.UTF8.GetBytes(htmlContent);
            return Task.FromResult(bytes);
        }

        public async Task<string> SaveReportAsync(ReportResult reportResult, string reportName, ReportType reportType)
        {
            try
            {
                var report = new Report
                {
                    ReportName = reportName,
                    Type = reportType,
                    GeneratedDate = DateTime.Now,
                    DateFrom = reportResult.FromDate,
                    DateTo = reportResult.ToDate,
                    TotalAmount = reportResult.Summary.TotalAmount,
                    TotalClaims = reportResult.Summary.TotalClaims,
                    FilePath = $"reports/{reportName}_{DateTime.Now:yyyyMMddHHmmss}.json"
                };

                _context.Reports.Add(report);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Report saved with ID {ReportId}", report.Id);
                return $"Report saved successfully with ID: {report.Id}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving report");
                return "Error saving report";
            }
        }

        public async Task<List<Report>> GetRecentReportsAsync(int count = 10)
        {
            return await _context.Reports
                .OrderByDescending(r => r.GeneratedDate)
                .Take(count)
                .ToListAsync();
        }

        private ReportSummary CalculateSummary(List<Claim> claims)
        {
            return new ReportSummary
            {
                TotalClaims = claims.Count,
                TotalAmount = claims.Sum(c => c.TotalAmount),
                ApprovedClaims = claims.Count(c => c.Status == ClaimStatus.Approved),
                PendingClaims = claims.Count(c => c.Status == ClaimStatus.Pending),
                RejectedClaims = claims.Count(c => c.Status == ClaimStatus.Rejected),
                AverageClaimAmount = claims.Count > 0 ? claims.Average(c => c.TotalAmount) : 0,
                UniqueLecturers = claims.Select(c => c.LecturerId).Distinct().Count(),
                DepartmentsCount = claims.Where(c => c.Lecturer != null && !string.IsNullOrEmpty(c.Lecturer.Department))
                                       .Select(c => c.Lecturer!.Department)
                                       .Distinct()
                                       .Count()
            };
        }

        private string GenerateCsvContent(ReportResult reportResult)
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine($"Report: {reportResult.ReportTitle}");
            sb.AppendLine($"Period: {reportResult.FromDate:yyyy-MM-dd} to {reportResult.ToDate:yyyy-MM-dd}");
            sb.AppendLine($"Generated: {reportResult.GeneratedDate:yyyy-MM-dd HH:mm}");
            sb.AppendLine();

            // Summary
            sb.AppendLine("SUMMARY");
            sb.AppendLine($"Total Claims,{reportResult.Summary.TotalClaims}");
            sb.AppendLine($"Total Amount,{reportResult.Summary.TotalAmount:C}");
            sb.AppendLine($"Approved Claims,{reportResult.Summary.ApprovedClaims}");
            sb.AppendLine($"Pending Claims,{reportResult.Summary.PendingClaims}");
            sb.AppendLine($"Rejected Claims,{reportResult.Summary.RejectedClaims}");
            sb.AppendLine($"Average Claim Amount,{reportResult.Summary.AverageClaimAmount:C}");
            sb.AppendLine($"Unique Lecturers,{reportResult.Summary.UniqueLecturers}");
            sb.AppendLine($"Departments,{reportResult.Summary.DepartmentsCount}");
            sb.AppendLine();

            // Data
            sb.AppendLine("DETAILED DATA");
            sb.AppendLine("Category,SubCategory,Claim Count,Total Amount,Average Amount");

            foreach (var item in reportResult.Data)
            {
                sb.AppendLine($"{item.Category},{item.SubCategory},{item.ClaimCount},{item.TotalAmount:C},{item.AverageAmount:C}");
            }

            return sb.ToString();
        }

        private string GenerateHtmlContent(ReportResult reportResult)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>{reportResult.ReportTitle}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .summary {{ background: #f8f9fa; padding: 15px; border-radius: 5px; margin-bottom: 20px; }}
        .table {{ width: 100%; border-collapse: collapse; }}
        .table th, .table td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        .table th {{ background-color: #f2f2f2; }}
        .footer {{ margin-top: 30px; text-align: center; color: #666; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>{reportResult.ReportTitle}</h1>
        <p>Period: {reportResult.FromDate:yyyy-MM-dd} to {reportResult.ToDate:yyyy-MM-dd}</p>
        <p>Generated: {reportResult.GeneratedDate:yyyy-MM-dd HH:mm}</p>
    </div>
    
    <div class='summary'>
        <h3>Summary</h3>
        <p><strong>Total Claims:</strong> {reportResult.Summary.TotalClaims}</p>
        <p><strong>Total Amount:</strong> {reportResult.Summary.TotalAmount:C}</p>
        <p><strong>Approved Claims:</strong> {reportResult.Summary.ApprovedClaims}</p>
        <p><strong>Pending Claims:</strong> {reportResult.Summary.PendingClaims}</p>
        <p><strong>Rejected Claims:</strong> {reportResult.Summary.RejectedClaims}</p>
        <p><strong>Average Claim Amount:</strong> {reportResult.Summary.AverageClaimAmount:C}</p>
        <p><strong>Unique Lecturers:</strong> {reportResult.Summary.UniqueLecturers}</p>
        <p><strong>Departments:</strong> {reportResult.Summary.DepartmentsCount}</p>
    </div>
    
    <h3>Detailed Data</h3>
    <table class='table'>
        <thead>
            <tr>
                <th>Category</th>
                <th>SubCategory</th>
                <th>Claim Count</th>
                <th>Total Amount</th>
                <th>Average Amount</th>
            </tr>
        </thead>
        <tbody>";

            foreach (var item in reportResult.Data)
            {
                html += $@"
            <tr>
                <td>{item.Category}</td>
                <td>{item.SubCategory}</td>
                <td>{item.ClaimCount}</td>
                <td>{item.TotalAmount:C}</td>
                <td>{item.AverageAmount:C}</td>
            </tr>";
            }

            html += @"
        </tbody>
    </table>
    
    <div class='footer'>
        <p>Generated by CMCS - Contract Monthly Claim System</p>
    </div>
</body>
</html>";

            return html;
        }
    }
}