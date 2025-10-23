using EvCoOwnership.Repositories.Enums;
using FluentValidation;

namespace EvCoOwnership.Repositories.DTOs.FairnessOptimizationDTOs
{
    #region Request DTOs

    /// <summary>
    /// Request for generating comprehensive fairness report
    /// </summary>
    public class GetFairnessReportRequest
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IncludeRecommendations { get; set; } = true;
    }

    /// <summary>
    /// Request for fair usage schedule suggestions
    /// </summary>
    public class GetFairScheduleSuggestionsRequest
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? PreferredDurationHours { get; set; }
        public EUsageType? UsageType { get; set; }
    }

    /// <summary>
    /// Request for maintenance suggestions
    /// </summary>
    public class GetMaintenanceSuggestionsRequest
    {
        public bool IncludePredictive { get; set; } = true;
        public int LookaheadDays { get; set; } = 30;
    }

    /// <summary>
    /// Request for cost-saving recommendations
    /// </summary>
    public class GetCostSavingRecommendationsRequest
    {
        public int AnalysisPeriodDays { get; set; } = 90;
        public bool IncludeFundOptimization { get; set; } = true;
        public bool IncludeMaintenanceOptimization { get; set; } = true;
    }

    #endregion

    #region Fairness Report DTOs

    /// <summary>
    /// Comprehensive fairness report for a vehicle
    /// </summary>
    public class FairnessReportResponse
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public DateTime ReportStartDate { get; set; }
        public DateTime ReportEndDate { get; set; }

        public FairnessOverview Overview { get; set; } = new();
        public List<CoOwnerFairnessDetail> CoOwnersDetails { get; set; } = new();
        public List<FairnessRecommendation> Recommendations { get; set; } = new();
        public FairnessMetrics Metrics { get; set; } = new();
        
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Overall fairness overview
    /// </summary>
    public class FairnessOverview
    {
        public string OverallFairnessStatus { get; set; } = string.Empty; // "Excellent", "Good", "Fair", "Poor"
        public decimal FairnessScore { get; set; } // 0-100
        public decimal AverageUsageVariance { get; set; }
        public int BalancedCoOwnersCount { get; set; }
        public int OverutilizedCoOwnersCount { get; set; }
        public int UnderutilizedCoOwnersCount { get; set; }
        public string MainIssue { get; set; } = string.Empty;
    }

    /// <summary>
    /// Fairness details for individual co-owner
    /// </summary>
    public class CoOwnerFairnessDetail
    {
        public int CoOwnerId { get; set; }
        public int UserId { get; set; }
        public string CoOwnerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Ownership
        public decimal OwnershipPercentage { get; set; }
        public decimal InvestmentAmount { get; set; }

        // Usage
        public decimal UsageHoursPercentage { get; set; }
        public decimal UsageDistancePercentage { get; set; }
        public decimal UsageBookingsPercentage { get; set; }
        public decimal AverageUsagePercentage { get; set; }

        // Fairness
        public decimal UsageVsOwnershipDelta { get; set; }
        public string UsagePattern { get; set; } = string.Empty;
        public decimal FairnessScore { get; set; } // 0-100

        // Financial
        public decimal ExpectedCostShare { get; set; } // Based on ownership
        public decimal ActualCostShare { get; set; } // Based on usage
        public decimal CostAdjustmentNeeded { get; set; }

        // Recommendations
        public List<string> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// Fairness metrics and statistics
    /// </summary>
    public class FairnessMetrics
    {
        public decimal TotalUsageHours { get; set; }
        public decimal TotalUsageDistance { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalMaintenanceCost { get; set; }
        public decimal TotalFundBalance { get; set; }
        
        public decimal UsageVariance { get; set; }
        public decimal CostVariance { get; set; }
        public decimal OptimalRebalanceFrequencyDays { get; set; }
    }

    /// <summary>
    /// Actionable fairness recommendation
    /// </summary>
    public class FairnessRecommendation
    {
        public string Type { get; set; } = string.Empty; // "Usage", "Cost", "Schedule", "Ownership"
        public string Priority { get; set; } = string.Empty; // "High", "Medium", "Low"
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> ActionItems { get; set; } = new();
        public decimal? ExpectedImpact { get; set; }
        public List<int> AffectedCoOwnerIds { get; set; } = new();
    }

    #endregion

    #region Fair Schedule Suggestions DTOs

    /// <summary>
    /// Fair usage schedule suggestions response
    /// </summary>
    public class FairScheduleSuggestionsResponse
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public DateTime SuggestionPeriodStart { get; set; }
        public DateTime SuggestionPeriodEnd { get; set; }

        public List<CoOwnerScheduleSuggestion> CoOwnerSuggestions { get; set; } = new();
        public List<OptimalTimeSlot> OptimalTimeSlots { get; set; } = new();
        public ScheduleOptimizationInsights Insights { get; set; } = new();

        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Schedule suggestion for individual co-owner
    /// </summary>
    public class CoOwnerScheduleSuggestion
    {
        public int CoOwnerId { get; set; }
        public string CoOwnerName { get; set; } = string.Empty;
        
        public decimal OwnershipPercentage { get; set; }
        public decimal CurrentUsagePercentage { get; set; }
        public decimal RecommendedUsagePercentage { get; set; }
        
        public int SuggestedBookingsCount { get; set; }
        public decimal SuggestedTotalHours { get; set; }
        public List<SuggestedBookingSlot> SuggestedSlots { get; set; } = new();
        
        public string Rationale { get; set; } = string.Empty;
    }

    /// <summary>
    /// Individual suggested booking slot
    /// </summary>
    public class SuggestedBookingSlot
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal DurationHours { get; set; }
        public string Reason { get; set; } = string.Empty;
        public decimal ConflictProbability { get; set; } // 0-1
        public List<string> Benefits { get; set; } = new();
    }

    /// <summary>
    /// Optimal time slots for vehicle usage
    /// </summary>
    public class OptimalTimeSlot
    {
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal UtilizationRate { get; set; }
        public string PeakType { get; set; } = string.Empty; // "Low", "Medium", "High"
        public List<int> RecommendedForCoOwnerIds { get; set; } = new();
    }

    /// <summary>
    /// Schedule optimization insights
    /// </summary>
    public class ScheduleOptimizationInsights
    {
        public decimal CurrentUtilizationRate { get; set; }
        public decimal OptimalUtilizationRate { get; set; }
        public int ConflictingBookingsCount { get; set; }
        public List<string> PeakUsagePeriods { get; set; } = new();
        public List<string> UnderutilizedPeriods { get; set; } = new();
        public decimal PotentialEfficiencyGain { get; set; }
    }

    #endregion

    #region Maintenance Suggestions DTOs

    /// <summary>
    /// Maintenance suggestions response
    /// </summary>
    public class MaintenanceSuggestionsResponse
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;

        public VehicleHealthStatus HealthStatus { get; set; } = new();
        public List<MaintenanceSuggestion> Suggestions { get; set; } = new();
        public List<UpcomingMaintenance> UpcomingMaintenance { get; set; } = new();
        public MaintenanceCostForecast CostForecast { get; set; } = new();

        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Current vehicle health status
    /// </summary>
    public class VehicleHealthStatus
    {
        public int CurrentOdometer { get; set; }
        public decimal AverageDailyDistance { get; set; }
        public int DaysSinceLastMaintenance { get; set; }
        public int DistanceSinceLastMaintenance { get; set; }
        
        public string OverallHealth { get; set; } = string.Empty; // "Excellent", "Good", "Fair", "Poor"
        public int HealthScore { get; set; } // 0-100
        public List<string> HealthIssues { get; set; } = new();
    }

    /// <summary>
    /// Individual maintenance suggestion
    /// </summary>
    public class MaintenanceSuggestion
    {
        public EMaintenanceType MaintenanceType { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Urgency { get; set; } = string.Empty; // "Critical", "High", "Medium", "Low"
        public string Reason { get; set; } = string.Empty;
        
        public int? RecommendedOdometerReading { get; set; }
        public DateTime? RecommendedDate { get; set; }
        public int DaysUntilRecommended { get; set; }
        
        public decimal EstimatedCost { get; set; }
        public decimal CostSavingIfDoneNow { get; set; }
        
        public List<string> Consequences { get; set; } = new();
        public List<string> Benefits { get; set; } = new();
    }

    /// <summary>
    /// Upcoming scheduled maintenance
    /// </summary>
    public class UpcomingMaintenance
    {
        public EMaintenanceType MaintenanceType { get; set; }
        public DateTime DueDate { get; set; }
        public int DaysUntilDue { get; set; }
        public int? OdometerDue { get; set; }
        public decimal EstimatedCost { get; set; }
        public bool IsOverdue { get; set; }
    }

    /// <summary>
    /// Maintenance cost forecast
    /// </summary>
    public class MaintenanceCostForecast
    {
        public int ForecastPeriodDays { get; set; }
        public decimal EstimatedTotalCost { get; set; }
        public decimal AverageMonthlyCost { get; set; }
        public decimal CostPerCoOwnerAverage { get; set; }
        
        public List<MonthlyMaintenanceForecast> MonthlyForecasts { get; set; } = new();
        public List<string> CostDrivers { get; set; } = new();
    }

    /// <summary>
    /// Monthly maintenance cost forecast
    /// </summary>
    public class MonthlyMaintenanceForecast
    {
        public string Month { get; set; } = string.Empty;
        public decimal EstimatedCost { get; set; }
        public List<string> ExpectedMaintenanceTypes { get; set; } = new();
    }

    #endregion

    #region Cost-Saving Recommendations DTOs

    /// <summary>
    /// Cost-saving recommendations response
    /// </summary>
    public class CostSavingRecommendationsResponse
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        
        public CostAnalysisSummary Summary { get; set; } = new();
        public List<CostSavingRecommendation> Recommendations { get; set; } = new();
        public FundOptimizationInsights FundInsights { get; set; } = new();
        public MaintenanceOptimizationInsights MaintenanceInsights { get; set; } = new();

        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Cost analysis summary
    /// </summary>
    public class CostAnalysisSummary
    {
        public int AnalysisPeriodDays { get; set; }
        public decimal TotalCostsIncurred { get; set; }
        public decimal AverageMonthlyCost { get; set; }
        public decimal CostPerKm { get; set; }
        public decimal CostPerBooking { get; set; }
        
        public decimal PotentialSavings { get; set; }
        public decimal SavingsPercentage { get; set; }
        
        public List<CostBreakdown> CostBreakdowns { get; set; } = new();
    }

    /// <summary>
    /// Cost breakdown by category
    /// </summary>
    public class CostBreakdown
    {
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Percentage { get; set; }
        public string Trend { get; set; } = string.Empty; // "Increasing", "Stable", "Decreasing"
    }

    /// <summary>
    /// Individual cost-saving recommendation
    /// </summary>
    public class CostSavingRecommendation
    {
        public string Category { get; set; } = string.Empty; // "Maintenance", "Fund", "Usage", "General"
        public string Priority { get; set; } = string.Empty; // "High", "Medium", "Low"
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        public decimal PotentialSavingsAmount { get; set; }
        public decimal PotentialSavingsPercentage { get; set; }
        public string TimeframeForSavings { get; set; } = string.Empty;
        
        public List<string> ActionSteps { get; set; } = new();
        public string Difficulty { get; set; } = string.Empty; // "Easy", "Medium", "Hard"
        public decimal ImplementationCost { get; set; }
        public decimal ROI { get; set; }
    }

    /// <summary>
    /// Fund optimization insights
    /// </summary>
    public class FundOptimizationInsights
    {
        public decimal CurrentFundBalance { get; set; }
        public decimal RecommendedMinimumBalance { get; set; }
        public decimal RecommendedOptimalBalance { get; set; }
        
        public bool IsUnderfunded { get; set; }
        public bool IsOverfunded { get; set; }
        
        public decimal AverageMonthlyExpenses { get; set; }
        public int MonthsCovered { get; set; }
        
        public List<string> FundHealthIssues { get; set; } = new();
        public List<string> FundOptimizationTips { get; set; } = new();
    }

    /// <summary>
    /// Maintenance optimization insights
    /// </summary>
    public class MaintenanceOptimizationInsights
    {
        public decimal AverageMaintenanceCost { get; set; }
        public decimal PreventiveMaintenanceRatio { get; set; }
        public decimal ReactiveMaintenanceRatio { get; set; }
        
        public decimal PotentialSavingsFromPreventive { get; set; }
        public List<string> HighCostMaintenanceTypes { get; set; } = new();
        
        public bool HasMaintenanceSchedule { get; set; }
        public List<string> OptimizationOpportunities { get; set; } = new();
    }

    #endregion

    #region Validators

    public class GetFairScheduleSuggestionsRequestValidator : AbstractValidator<GetFairScheduleSuggestionsRequest>
    {
        public GetFairScheduleSuggestionsRequestValidator()
        {
            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("START_DATE_REQUIRED")
                .LessThan(x => x.EndDate).WithMessage("START_DATE_MUST_BE_BEFORE_END_DATE");

            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage("END_DATE_REQUIRED")
                .GreaterThan(DateTime.UtcNow).WithMessage("END_DATE_MUST_BE_IN_FUTURE");

            RuleFor(x => x.PreferredDurationHours)
                .GreaterThan(0).When(x => x.PreferredDurationHours.HasValue)
                .WithMessage("DURATION_MUST_BE_POSITIVE");
        }
    }

    public class GetMaintenanceSuggestionsRequestValidator : AbstractValidator<GetMaintenanceSuggestionsRequest>
    {
        public GetMaintenanceSuggestionsRequestValidator()
        {
            RuleFor(x => x.LookaheadDays)
                .InclusiveBetween(1, 365)
                .WithMessage("LOOKAHEAD_DAYS_MUST_BE_BETWEEN_1_AND_365");
        }
    }

    public class GetCostSavingRecommendationsRequestValidator : AbstractValidator<GetCostSavingRecommendationsRequest>
    {
        public GetCostSavingRecommendationsRequestValidator()
        {
            RuleFor(x => x.AnalysisPeriodDays)
                .InclusiveBetween(7, 365)
                .WithMessage("ANALYSIS_PERIOD_MUST_BE_BETWEEN_7_AND_365_DAYS");
        }
    }

    #endregion
}
