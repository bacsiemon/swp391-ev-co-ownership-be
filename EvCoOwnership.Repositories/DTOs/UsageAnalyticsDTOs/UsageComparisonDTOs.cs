using FluentValidation;

namespace EvCoOwnership.Repositories.DTOs.UsageAnalyticsDTOs
{
    #region Request DTOs

    /// <summary>
    /// Request to compare usage across multiple co-owners over time
    /// </summary>
    public class CompareCoOwnersUsageRequest
    {
        /// <summary>
        /// Vehicle ID to compare usage for
        /// </summary>
        public int VehicleId { get; set; }

        /// <summary>
        /// Start date for comparison (optional, defaults to 3 months ago)
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// End date for comparison (optional, defaults to current date)
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Time granularity: Daily, Weekly, Monthly (default: Weekly)
        /// </summary>
        public string Granularity { get; set; } = "Weekly";

        /// <summary>
        /// Metrics to compare: Hours, Distance, BookingCount, All (default: All)
        /// </summary>
        public string Metrics { get; set; } = "All";

        /// <summary>
        /// Specific co-owner IDs to compare (optional, compares all if empty)
        /// </summary>
        public List<int>? CoOwnerIds { get; set; }
    }

    /// <summary>
    /// Request to compare usage across multiple vehicles over time
    /// </summary>
    public class CompareVehiclesUsageRequest
    {
        /// <summary>
        /// Vehicle IDs to compare (required, at least 2 vehicles)
        /// </summary>
        public List<int> VehicleIds { get; set; } = new();

        /// <summary>
        /// Start date for comparison (optional, defaults to 3 months ago)
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// End date for comparison (optional, defaults to current date)
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Time granularity: Daily, Weekly, Monthly (default: Weekly)
        /// </summary>
        public string Granularity { get; set; } = "Weekly";

        /// <summary>
        /// Metrics to compare: Hours, Distance, BookingCount, UtilizationRate, All (default: All)
        /// </summary>
        public string Metrics { get; set; } = "All";
    }

    /// <summary>
    /// Request to compare personal usage metrics over different time periods
    /// </summary>
    public class ComparePeriodUsageRequest
    {
        /// <summary>
        /// Vehicle ID (optional, compares all vehicles if null)
        /// </summary>
        public int? VehicleId { get; set; }

        /// <summary>
        /// First period start date
        /// </summary>
        public DateTime Period1Start { get; set; }

        /// <summary>
        /// First period end date
        /// </summary>
        public DateTime Period1End { get; set; }

        /// <summary>
        /// Second period start date
        /// </summary>
        public DateTime Period2Start { get; set; }

        /// <summary>
        /// Second period end date
        /// </summary>
        public DateTime Period2End { get; set; }

        /// <summary>
        /// Period 1 label (e.g., "Q1 2024", "Last Month")
        /// </summary>
        public string? Period1Label { get; set; }

        /// <summary>
        /// Period 2 label (e.g., "Q2 2024", "This Month")
        /// </summary>
        public string? Period2Label { get; set; }
    }

    #endregion

    #region Response DTOs

    /// <summary>
    /// Response containing co-owners usage comparison over time
    /// </summary>
    public class CoOwnersUsageComparisonResponse
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;

        // Time-series data for each co-owner
        public List<CoOwnerUsageTimeSeries> CoOwnersSeries { get; set; } = new();

        // Aggregate statistics
        public UsageComparisonStatistics Statistics { get; set; } = new();

        // Rankings and insights
        public List<CoOwnerRanking> Rankings { get; set; } = new();
        public List<UsageInsight> Insights { get; set; } = new();

        // Period information
        public DateTime AnalysisStartDate { get; set; }
        public DateTime AnalysisEndDate { get; set; }
        public string Granularity { get; set; } = string.Empty;
        public int TotalPeriods { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Time-series usage data for a single co-owner
    /// </summary>
    public class CoOwnerUsageTimeSeries
    {
        public int CoOwnerId { get; set; }
        public string CoOwnerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal OwnershipPercentage { get; set; }

        // Time-series data points
        public List<UsageDataPoint> DataPoints { get; set; } = new();

        // Trend analysis
        public TrendAnalysis Trend { get; set; } = new();

        // Summary statistics
        public decimal TotalHours { get; set; }
        public decimal TotalDistance { get; set; }
        public int TotalBookings { get; set; }
        public decimal AveragePerPeriod { get; set; }
        public decimal PeakUsage { get; set; }
        public string PeakPeriod { get; set; } = string.Empty;
    }

    /// <summary>
    /// Single data point in time-series
    /// </summary>
    public class UsageDataPoint
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string PeriodLabel { get; set; } = string.Empty;

        public decimal Hours { get; set; }
        public decimal Distance { get; set; }
        public int BookingCount { get; set; }
        public decimal UtilizationRate { get; set; }

        // Comparison with previous period
        public decimal? HoursChange { get; set; }
        public decimal? DistanceChange { get; set; }
        public int? BookingCountChange { get; set; }
    }

    /// <summary>
    /// Trend analysis for a time-series
    /// </summary>
    public class TrendAnalysis
    {
        public string Direction { get; set; } = string.Empty; // Increasing, Decreasing, Stable, Volatile
        public decimal GrowthRate { get; set; } // Percentage growth from first to last period
        public decimal AverageChange { get; set; } // Average change between consecutive periods
        public decimal Volatility { get; set; } // Standard deviation of changes
        public bool IsConsistent { get; set; } // Low volatility
        public string Pattern { get; set; } = string.Empty; // Linear, Exponential, Seasonal, Random
    }

    /// <summary>
    /// Overall comparison statistics
    /// </summary>
    public class UsageComparisonStatistics
    {
        public decimal TotalHoursAllCoOwners { get; set; }
        public decimal TotalDistanceAllCoOwners { get; set; }
        public int TotalBookingsAllCoOwners { get; set; }

        public decimal AverageHoursPerCoOwner { get; set; }
        public decimal AverageDistancePerCoOwner { get; set; }
        public decimal AverageBookingsPerCoOwner { get; set; }

        // Dispersion metrics
        public decimal UsageDispersion { get; set; } // How evenly distributed is usage
        public decimal GiniCoefficient { get; set; } // 0 = perfect equality, 1 = perfect inequality

        // Most/Least active
        public string MostActiveCoOwner { get; set; } = string.Empty;
        public decimal MostActiveHours { get; set; }
        public string LeastActiveCoOwner { get; set; } = string.Empty;
        public decimal LeastActiveHours { get; set; }

        // Growth comparison
        public string FastestGrowingCoOwner { get; set; } = string.Empty;
        public decimal FastestGrowthRate { get; set; }
    }

    /// <summary>
    /// Co-owner ranking in various metrics
    /// </summary>
    public class CoOwnerRanking
    {
        public string Metric { get; set; } = string.Empty; // Hours, Distance, BookingCount, GrowthRate
        public List<RankingEntry> Rankings { get; set; } = new();
    }

    /// <summary>
    /// Single ranking entry
    /// </summary>
    public class RankingEntry
    {
        public int Rank { get; set; }
        public int CoOwnerId { get; set; }
        public string CoOwnerName { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal PercentageOfTotal { get; set; }
    }

    /// <summary>
    /// Usage insight or recommendation
    /// </summary>
    public class UsageInsight
    {
        public string Type { get; set; } = string.Empty; // Imbalance, UnusualPattern, Recommendation
        public string Severity { get; set; } = string.Empty; // Info, Warning, Critical
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> AffectedCoOwners { get; set; } = new();
        public Dictionary<string, object> Data { get; set; } = new();
    }

    /// <summary>
    /// Response containing vehicles usage comparison over time
    /// </summary>
    public class VehiclesUsageComparisonResponse
    {
        // Time-series data for each vehicle
        public List<VehicleUsageTimeSeries> VehiclesSeries { get; set; } = new();

        // Comparative statistics
        public VehicleComparisonStatistics Statistics { get; set; } = new();

        // Rankings
        public List<VehicleRanking> Rankings { get; set; } = new();

        // Insights
        public List<UsageInsight> Insights { get; set; } = new();

        // Period information
        public DateTime AnalysisStartDate { get; set; }
        public DateTime AnalysisEndDate { get; set; }
        public string Granularity { get; set; } = string.Empty;
        public int TotalPeriods { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Time-series usage data for a single vehicle
    /// </summary>
    public class VehicleUsageTimeSeries
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;

        // Time-series data points
        public List<UsageDataPoint> DataPoints { get; set; } = new();

        // Trend analysis
        public TrendAnalysis Trend { get; set; } = new();

        // Summary statistics
        public decimal TotalHours { get; set; }
        public decimal TotalDistance { get; set; }
        public int TotalBookings { get; set; }
        public decimal AverageUtilization { get; set; }
        public decimal PeakUtilization { get; set; }
        public string PeakPeriod { get; set; } = string.Empty;
    }

    /// <summary>
    /// Vehicle comparison statistics
    /// </summary>
    public class VehicleComparisonStatistics
    {
        public decimal TotalHoursAllVehicles { get; set; }
        public decimal TotalDistanceAllVehicles { get; set; }
        public int TotalBookingsAllVehicles { get; set; }

        public decimal AverageHoursPerVehicle { get; set; }
        public decimal AverageDistancePerVehicle { get; set; }
        public decimal AverageUtilizationRate { get; set; }

        // Most/Least utilized
        public string MostUtilizedVehicle { get; set; } = string.Empty;
        public decimal MostUtilizedHours { get; set; }
        public string LeastUtilizedVehicle { get; set; } = string.Empty;
        public decimal LeastUtilizedHours { get; set; }

        // Efficiency comparison
        public string MostEfficientVehicle { get; set; } = string.Empty; // Best utilization rate
        public decimal BestUtilizationRate { get; set; }
    }

    /// <summary>
    /// Vehicle ranking in various metrics
    /// </summary>
    public class VehicleRanking
    {
        public string Metric { get; set; } = string.Empty;
        public List<VehicleRankingEntry> Rankings { get; set; } = new();
    }

    /// <summary>
    /// Single vehicle ranking entry
    /// </summary>
    public class VehicleRankingEntry
    {
        public int Rank { get; set; }
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal PercentageOfTotal { get; set; }
    }

    /// <summary>
    /// Response containing period-to-period usage comparison
    /// </summary>
    public class PeriodUsageComparisonResponse
    {
        // Period 1 data
        public PeriodUsageData Period1 { get; set; } = new();

        // Period 2 data
        public PeriodUsageData Period2 { get; set; } = new();

        // Comparison metrics
        public PeriodComparisonMetrics Comparison { get; set; } = new();

        // Vehicle breakdown (if single vehicle)
        public VehiclePeriodComparison? VehicleComparison { get; set; }

        // Insights
        public List<UsageInsight> Insights { get; set; } = new();

        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Usage data for a single period
    /// </summary>
    public class PeriodUsageData
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Label { get; set; } = string.Empty;
        public int DurationDays { get; set; }

        public decimal TotalHours { get; set; }
        public decimal TotalDistance { get; set; }
        public int TotalBookings { get; set; }
        public decimal AverageBookingDuration { get; set; }
        public decimal AverageTripDistance { get; set; }
        public decimal UtilizationRate { get; set; }

        // Breakdown by vehicle (if comparing all vehicles)
        public List<VehicleUsageSummary> VehicleBreakdown { get; set; } = new();

        // Most active day/time
        public string MostActiveDay { get; set; } = string.Empty;
        public string MostActiveTimeSlot { get; set; } = string.Empty;
    }

    /// <summary>
    /// Comparison metrics between two periods
    /// </summary>
    public class PeriodComparisonMetrics
    {
        // Absolute changes
        public decimal HoursChange { get; set; }
        public decimal DistanceChange { get; set; }
        public int BookingCountChange { get; set; }
        public decimal UtilizationRateChange { get; set; }

        // Percentage changes
        public decimal HoursChangePercentage { get; set; }
        public decimal DistanceChangePercentage { get; set; }
        public decimal BookingCountChangePercentage { get; set; }
        public decimal UtilizationRateChangePercentage { get; set; }

        // Normalized changes (per day)
        public decimal HoursPerDayChange { get; set; }
        public decimal DistancePerDayChange { get; set; }
        public decimal BookingsPerDayChange { get; set; }

        // Overall trend
        public string OverallTrend { get; set; } = string.Empty; // Increased, Decreased, Stable
        public string TrendStrength { get; set; } = string.Empty; // Significant, Moderate, Slight
    }

    /// <summary>
    /// Vehicle-specific period comparison
    /// </summary>
    public class VehiclePeriodComparison
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;

        // Co-owner changes
        public List<CoOwnerPeriodChange> CoOwnerChanges { get; set; } = new();

        // Booking pattern changes
        public BookingPatternComparison PatternComparison { get; set; } = new();
    }

    /// <summary>
    /// Co-owner usage change between periods
    /// </summary>
    public class CoOwnerPeriodChange
    {
        public int CoOwnerId { get; set; }
        public string CoOwnerName { get; set; } = string.Empty;

        public decimal Period1Hours { get; set; }
        public decimal Period2Hours { get; set; }
        public decimal HoursChange { get; set; }
        public decimal HoursChangePercentage { get; set; }

        public string ChangeDirection { get; set; } = string.Empty; // Increased, Decreased, Stable
    }

    /// <summary>
    /// Booking pattern comparison between periods
    /// </summary>
    public class BookingPatternComparison
    {
        // Day of week distribution changes
        public Dictionary<string, decimal> Period1DayDistribution { get; set; } = new();
        public Dictionary<string, decimal> Period2DayDistribution { get; set; } = new();

        // Time slot distribution changes
        public Dictionary<string, int> Period1TimeDistribution { get; set; } = new();
        public Dictionary<string, int> Period2TimeDistribution { get; set; } = new();

        // Pattern insights
        public List<string> PatternChanges { get; set; } = new();
    }

    #endregion

    #region Validators

    /// <summary>
    /// Validator for CompareCoOwnersUsageRequest
    /// </summary>
    public class CompareCoOwnersUsageRequestValidator : AbstractValidator<CompareCoOwnersUsageRequest>
    {
        public CompareCoOwnersUsageRequestValidator()
        {
            RuleFor(x => x.VehicleId)
                .GreaterThan(0)
                .WithMessage("VehicleId must be greater than 0");

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate)
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                .WithMessage("End date must be after start date");

            RuleFor(x => x.Granularity)
                .Must(x => string.IsNullOrEmpty(x) || new[] { "Daily", "Weekly", "Monthly" }.Contains(x))
                .WithMessage("Granularity must be one of: Daily, Weekly, Monthly");

            RuleFor(x => x.Metrics)
                .Must(x => string.IsNullOrEmpty(x) || new[] { "Hours", "Distance", "BookingCount", "All" }.Contains(x))
                .WithMessage("Metrics must be one of: Hours, Distance, BookingCount, All");
        }
    }

    /// <summary>
    /// Validator for CompareVehiclesUsageRequest
    /// </summary>
    public class CompareVehiclesUsageRequestValidator : AbstractValidator<CompareVehiclesUsageRequest>
    {
        public CompareVehiclesUsageRequestValidator()
        {
            RuleFor(x => x.VehicleIds)
                .NotEmpty()
                .WithMessage("At least one vehicle ID is required")
                .Must(x => x.Count >= 2)
                .WithMessage("At least 2 vehicles are required for comparison");

            RuleFor(x => x.VehicleIds)
                .Must(x => x.Distinct().Count() == x.Count)
                .WithMessage("Vehicle IDs must be unique");

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate)
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                .WithMessage("End date must be after start date");

            RuleFor(x => x.Granularity)
                .Must(x => string.IsNullOrEmpty(x) || new[] { "Daily", "Weekly", "Monthly" }.Contains(x))
                .WithMessage("Granularity must be one of: Daily, Weekly, Monthly");

            RuleFor(x => x.Metrics)
                .Must(x => string.IsNullOrEmpty(x) || new[] { "Hours", "Distance", "BookingCount", "UtilizationRate", "All" }.Contains(x))
                .WithMessage("Metrics must be one of: Hours, Distance, BookingCount, UtilizationRate, All");
        }
    }

    /// <summary>
    /// Validator for ComparePeriodUsageRequest
    /// </summary>
    public class ComparePeriodUsageRequestValidator : AbstractValidator<ComparePeriodUsageRequest>
    {
        public ComparePeriodUsageRequestValidator()
        {
            RuleFor(x => x.Period1End)
                .GreaterThan(x => x.Period1Start)
                .WithMessage("Period 1 end date must be after start date");

            RuleFor(x => x.Period2End)
                .GreaterThan(x => x.Period2Start)
                .WithMessage("Period 2 end date must be after start date");

            RuleFor(x => x.Period1Start)
                .LessThan(DateTime.UtcNow.AddDays(1))
                .WithMessage("Period 1 start date cannot be in the future");

            RuleFor(x => x.Period2Start)
                .LessThan(DateTime.UtcNow.AddDays(1))
                .WithMessage("Period 2 start date cannot be in the future");
        }
    }

    #endregion
}
