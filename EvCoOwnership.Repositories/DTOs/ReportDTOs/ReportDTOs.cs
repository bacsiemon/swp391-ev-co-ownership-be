using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Repositories.DTOs.ReportDTOs
{
    #region Request DTOs

    /// <summary>
    /// Request to generate monthly report for a vehicle
    /// </summary>
    public class GenerateMonthlyReportRequest
    {
        public int VehicleId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; } // 1-12
    }

    /// <summary>
    /// Request to generate quarterly report for a vehicle
    /// </summary>
    public class GenerateQuarterlyReportRequest
    {
        public int VehicleId { get; set; }
        public int Year { get; set; }
        public int Quarter { get; set; } // 1-4 (Q1, Q2, Q3, Q4)
    }

    /// <summary>
    /// Request to generate yearly report for a vehicle
    /// </summary>
    public class GenerateYearlyReportRequest
    {
        public int VehicleId { get; set; }
        public int Year { get; set; }
    }

    /// <summary>
    /// Request to export report to PDF or Excel
    /// </summary>
    public class ExportReportRequest
    {
        public int VehicleId { get; set; }
        public int Year { get; set; }
        public int? Month { get; set; } // Optional: for monthly reports
        public int? Quarter { get; set; } // Optional: for quarterly reports
        public string ExportFormat { get; set; } = "PDF"; // "PDF" or "Excel"
    }

    #endregion

    #region Response DTOs

    /// <summary>
    /// Summary of usage statistics for a period
    /// </summary>
    public class UsageSummary
    {
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal TotalHoursUsed { get; set; }
        public decimal TotalDistanceTraveled { get; set; } // in km
        public decimal AverageUsagePerBooking { get; set; } // in hours
        public List<CoOwnerUsageDetail> UsageByCoOwner { get; set; } = new();
        public List<UsageByDayOfWeek> UsageByDayOfWeek { get; set; } = new();
    }

    /// <summary>
    /// Usage details for a specific co-owner
    /// </summary>
    public class CoOwnerUsageDetail
    {
        public int CoOwnerId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int BookingCount { get; set; }
        public decimal TotalHours { get; set; }
        public decimal UsagePercentage { get; set; }
        public decimal TotalCost { get; set; }
    }

    /// <summary>
    /// Usage distribution by day of week
    /// </summary>
    public class UsageByDayOfWeek
    {
        public string DayOfWeek { get; set; } = string.Empty; // Monday, Tuesday, etc.
        public int BookingCount { get; set; }
        public decimal TotalHours { get; set; }
    }

    /// <summary>
    /// Summary of costs for a period
    /// </summary>
    public class CostSummary
    {
        public decimal TotalIncome { get; set; } // FundAdditions
        public decimal TotalExpenses { get; set; } // FundUsages
        public decimal NetBalance { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public List<ExpenseByCategory> ExpensesByCategory { get; set; } = new();
        public List<IncomeByCoOwner> IncomeByCoOwner { get; set; } = new();
        public List<MonthlyTrend> MonthlyTrends { get; set; } = new();
    }

    /// <summary>
    /// Expenses breakdown by category (UsageType)
    /// </summary>
    public class ExpenseByCategory
    {
        public EUsageType Category { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int TransactionCount { get; set; }
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// Income contributions by co-owner
    /// </summary>
    public class IncomeByCoOwner
    {
        public int CoOwnerId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public decimal TotalContribution { get; set; }
        public int ContributionCount { get; set; }
        public decimal ContributionPercentage { get; set; }
    }

    /// <summary>
    /// Monthly income/expense trend (for quarterly/yearly reports)
    /// </summary>
    public class MonthlyTrend
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
        public decimal NetChange { get; set; }
    }

    /// <summary>
    /// Comprehensive monthly report response
    /// </summary>
    public class MonthlyReportResponse
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public string PeriodDescription { get; set; } = string.Empty; // "January 2025"
        public DateTime GeneratedAt { get; set; }
        
        // Usage section
        public UsageSummary UsageSummary { get; set; } = new();
        
        // Cost section
        public CostSummary CostSummary { get; set; } = new();
        
        // Maintenance section
        public MaintenanceSummary MaintenanceSummary { get; set; } = new();
        
        // Fund status
        public FundStatus FundStatus { get; set; } = new();
    }

    /// <summary>
    /// Comprehensive quarterly report response
    /// </summary>
    public class QuarterlyReportResponse
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Quarter { get; set; }
        public string QuarterName { get; set; } = string.Empty; // "Q1", "Q2", etc.
        public string PeriodDescription { get; set; } = string.Empty; // "Q1 2025 (Jan-Mar)"
        public DateTime GeneratedAt { get; set; }
        
        // Usage section
        public UsageSummary UsageSummary { get; set; } = new();
        
        // Cost section
        public CostSummary CostSummary { get; set; } = new();
        
        // Maintenance section
        public MaintenanceSummary MaintenanceSummary { get; set; } = new();
        
        // Fund status
        public FundStatus FundStatus { get; set; } = new();
        
        // Monthly breakdown within quarter
        public List<MonthlyReportSummary> MonthlyBreakdown { get; set; } = new();
    }

    /// <summary>
    /// Comprehensive yearly report response
    /// </summary>
    public class YearlyReportResponse
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public int Year { get; set; }
        public string PeriodDescription { get; set; } = string.Empty; // "Year 2025"
        public DateTime GeneratedAt { get; set; }
        
        // Usage section
        public UsageSummary UsageSummary { get; set; } = new();
        
        // Cost section
        public CostSummary CostSummary { get; set; } = new();
        
        // Maintenance section
        public MaintenanceSummary MaintenanceSummary { get; set; } = new();
        
        // Fund status
        public FundStatus FundStatus { get; set; } = new();
        
        // Quarterly breakdown
        public List<QuarterlyReportSummary> QuarterlyBreakdown { get; set; } = new();
        
        // Monthly breakdown
        public List<MonthlyReportSummary> MonthlyBreakdown { get; set; } = new();
    }

    /// <summary>
    /// Summary of a single month (used in quarterly/yearly reports)
    /// </summary>
    public class MonthlyReportSummary
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int TotalBookings { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetChange { get; set; }
    }

    /// <summary>
    /// Summary of a single quarter (used in yearly reports)
    /// </summary>
    public class QuarterlyReportSummary
    {
        public int Year { get; set; }
        public int Quarter { get; set; }
        public string QuarterName { get; set; } = string.Empty;
        public int TotalBookings { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetChange { get; set; }
    }

    /// <summary>
    /// Maintenance activities summary
    /// </summary>
    public class MaintenanceSummary
    {
        public int TotalMaintenanceEvents { get; set; }
        public decimal TotalMaintenanceCost { get; set; }
        public List<MaintenanceByType> MaintenanceByType { get; set; } = new();
        public List<MaintenanceEvent> RecentMaintenanceEvents { get; set; } = new();
    }

    /// <summary>
    /// Maintenance breakdown by type
    /// </summary>
    public class MaintenanceByType
    {
        public EMaintenanceType MaintenanceType { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public int EventCount { get; set; }
        public decimal TotalCost { get; set; }
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// Individual maintenance event detail
    /// </summary>
    public class MaintenanceEvent
    {
        public int MaintenanceId { get; set; }
        public EMaintenanceType MaintenanceType { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public DateOnly ServiceDate { get; set; }
        public string ServiceProvider { get; set; } = string.Empty;
        public int? OdometerReading { get; set; }
    }

    /// <summary>
    /// Current fund status
    /// </summary>
    public class FundStatus
    {
        public int FundId { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal PeriodIncome { get; set; }
        public decimal PeriodExpenses { get; set; }
        public decimal PeriodNetChange { get; set; }
        public int TotalCoOwners { get; set; }
        public decimal AverageContributionPerCoOwner { get; set; }
    }

    /// <summary>
    /// Response for report export (PDF/Excel download)
    /// </summary>
    public class ExportReportResponse
    {
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public byte[] FileContent { get; set; } = Array.Empty<byte>();
        public long FileSizeBytes { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// List of available reports for a vehicle
    /// </summary>
    public class AvailableReportsResponse
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public List<ReportPeriod> AvailablePeriods { get; set; } = new();
    }

    /// <summary>
    /// Available report period
    /// </summary>
    public class ReportPeriod
    {
        public int Year { get; set; }
        public int? Month { get; set; }
        public int? Quarter { get; set; }
        public string PeriodDescription { get; set; } = string.Empty;
        public bool HasData { get; set; }
    }

    #endregion
}
