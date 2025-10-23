using FluentValidation;

namespace EvCoOwnership.Repositories.DTOs.UsageAnalyticsDTOs
{
    /// <summary>
    /// Request to get usage vs ownership graph data
    /// </summary>
    public class GetUsageVsOwnershipRequest
    {
        /// <summary>
        /// Start date for analysis (optional, defaults to vehicle creation date)
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// End date for analysis (optional, defaults to current date)
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Usage metric type: Hours, Distance, BookingCount (default: Hours)
        /// </summary>
        public string UsageMetric { get; set; } = "Hours";
    }

    /// <summary>
    /// Response containing usage vs ownership comparison data for a vehicle
    /// </summary>
    public class UsageVsOwnershipResponse
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public DateTime AnalysisStartDate { get; set; }
        public DateTime AnalysisEndDate { get; set; }
        public string UsageMetric { get; set; } = string.Empty;
        public List<CoOwnerUsageVsOwnership> CoOwnersData { get; set; } = new();
        public UsageOwnershipSummary Summary { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Usage vs ownership data for a single co-owner
    /// </summary>
    public class CoOwnerUsageVsOwnership
    {
        public int CoOwnerId { get; set; }
        public int UserId { get; set; }
        public string CoOwnerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Ownership data
        public decimal OwnershipPercentage { get; set; }
        public decimal InvestmentAmount { get; set; }

        // Usage data
        public decimal UsagePercentage { get; set; }
        public decimal ActualUsageValue { get; set; } // Hours, km, or count
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }

        // Comparison metrics
        public decimal UsageVsOwnershipDelta { get; set; } // UsagePercentage - OwnershipPercentage
        public string UsagePattern { get; set; } = string.Empty; // Underutilized, Balanced, Overutilized
        public decimal FairUsageValue { get; set; } // Expected usage based on ownership
    }

    /// <summary>
    /// Summary statistics for usage vs ownership analysis
    /// </summary>
    public class UsageOwnershipSummary
    {
        public decimal TotalUsageValue { get; set; } // Total hours, km, or bookings
        public decimal AverageOwnershipPercentage { get; set; }
        public decimal AverageUsagePercentage { get; set; }
        public decimal UsageVariance { get; set; } // Variance from expected usage
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public CoOwnerUsageVsOwnership? MostActiveCoOwner { get; set; }
        public CoOwnerUsageVsOwnership? LeastActiveCoOwner { get; set; }
        public int BalancedCoOwnersCount { get; set; }
        public int OverutilizedCoOwnersCount { get; set; }
        public int UnderutilizedCoOwnersCount { get; set; }
    }

    /// <summary>
    /// Time-series data for usage vs ownership trends
    /// </summary>
    public class UsageVsOwnershipTrendsResponse
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public DateTime AnalysisStartDate { get; set; }
        public DateTime AnalysisEndDate { get; set; }
        public string Granularity { get; set; } = string.Empty; // Daily, Weekly, Monthly
        public List<TrendDataPoint> TrendData { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Single time point in usage vs ownership trend
    /// </summary>
    public class TrendDataPoint
    {
        public DateTime Date { get; set; }
        public string Period { get; set; } = string.Empty; // Label (e.g., "Week 1", "Jan 2024")
        public List<CoOwnerTrendData> CoOwnersData { get; set; } = new();
    }

    /// <summary>
    /// Co-owner usage data for a specific time point
    /// </summary>
    public class CoOwnerTrendData
    {
        public int CoOwnerId { get; set; }
        public string CoOwnerName { get; set; } = string.Empty;
        public decimal OwnershipPercentage { get; set; }
        public decimal UsagePercentage { get; set; }
        public decimal UsageValue { get; set; }
    }

    /// <summary>
    /// Detailed breakdown of co-owner usage
    /// </summary>
    public class CoOwnerUsageDetailResponse
    {
        public int CoOwnerId { get; set; }
        public int UserId { get; set; }
        public string CoOwnerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;

        // Summary
        public decimal OwnershipPercentage { get; set; }
        public decimal UsagePercentage { get; set; }
        public decimal UsageVsOwnershipDelta { get; set; }

        // Usage breakdown by metric
        public UsageMetricsBreakdown UsageMetrics { get; set; } = new();

        // Booking details
        public List<BookingUsageSummary> RecentBookings { get; set; } = new();

        public DateTime AnalysisStartDate { get; set; }
        public DateTime AnalysisEndDate { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Breakdown of usage metrics
    /// </summary>
    public class UsageMetricsBreakdown
    {
        public decimal TotalHours { get; set; }
        public decimal HoursPercentage { get; set; }
        public decimal TotalDistance { get; set; }
        public decimal DistancePercentage { get; set; }
        public int TotalBookings { get; set; }
        public decimal BookingsPercentage { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal AverageBookingDuration { get; set; }
    }

    /// <summary>
    /// Summary of a booking for usage analysis
    /// </summary>
    public class BookingUsageSummary
    {
        public int BookingId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal DurationHours { get; set; }
        public int? DistanceTravelled { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
    }

    /// <summary>
    /// Validator for GetUsageVsOwnershipRequest
    /// </summary>
    public class GetUsageVsOwnershipRequestValidator : AbstractValidator<GetUsageVsOwnershipRequest>
    {
        public GetUsageVsOwnershipRequestValidator()
        {
            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate)
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                .WithMessage("End date must be after start date");

            RuleFor(x => x.UsageMetric)
                .Must(x => string.IsNullOrEmpty(x) || new[] { "Hours", "Distance", "BookingCount" }.Contains(x))
                .WithMessage("Usage metric must be one of: Hours, Distance, BookingCount");
        }
    }
}
