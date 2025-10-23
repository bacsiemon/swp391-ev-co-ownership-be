using FluentValidation;

namespace EvCoOwnership.Repositories.DTOs.UsageAnalyticsDTOs
{
    #region Request DTOs

    /// <summary>
    /// Request to get personal usage history
    /// </summary>
    public class GetPersonalUsageHistoryRequest
    {
        /// <summary>
        /// Start date for history (optional, defaults to 1 year ago)
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// End date for history (optional, defaults to current date)
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Filter by vehicle ID (optional, shows all vehicles if null)
        /// </summary>
        public int? VehicleId { get; set; }

        /// <summary>
        /// Filter by booking status: All, Completed, Cancelled, Pending (default: All)
        /// </summary>
        public string Status { get; set; } = "All";

        /// <summary>
        /// Page number for pagination (default: 1)
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Page size for pagination (default: 20)
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Sort by: StartTime, EndTime, DurationHours, Distance (default: StartTime)
        /// </summary>
        public string SortBy { get; set; } = "StartTime";

        /// <summary>
        /// Sort order: asc or desc (default: desc for newest first)
        /// </summary>
        public string SortOrder { get; set; } = "desc";
    }

    /// <summary>
    /// Request to get group usage summary
    /// </summary>
    public class GetGroupUsageSummaryRequest
    {
        /// <summary>
        /// Vehicle ID to get group summary for
        /// </summary>
        public int VehicleId { get; set; }

        /// <summary>
        /// Start date for summary (optional, defaults to vehicle creation)
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// End date for summary (optional, defaults to current date)
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Include detailed breakdown by time periods (default: true)
        /// </summary>
        public bool IncludeTimeBreakdown { get; set; } = true;

        /// <summary>
        /// Time granularity for breakdown: Daily, Weekly, Monthly (default: Monthly)
        /// </summary>
        public string Granularity { get; set; } = "Monthly";
    }

    #endregion

    #region Response DTOs

    /// <summary>
    /// Response containing personal usage history
    /// </summary>
    public class PersonalUsageHistoryResponse
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Summary statistics
        public PersonalUsageSummary Summary { get; set; } = new();

        // Paginated booking history
        public List<PersonalBookingHistory> Bookings { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();

        // Vehicle breakdown
        public List<VehicleUsageSummary> VehicleBreakdown { get; set; } = new();

        // Time period analysis
        public List<UsagePeriodStatistics> PeriodStatistics { get; set; } = new();

        public DateTime AnalysisStartDate { get; set; }
        public DateTime AnalysisEndDate { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Personal usage summary statistics
    /// </summary>
    public class PersonalUsageSummary
    {
        public int TotalVehicles { get; set; }
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public int PendingBookings { get; set; }

        public decimal TotalHoursUsed { get; set; }
        public decimal TotalDistanceTraveled { get; set; }
        public decimal AverageBookingDuration { get; set; }
        public decimal AverageTripDistance { get; set; }

        public decimal TotalCostPaid { get; set; }
        public decimal TotalInvestment { get; set; }

        // Peak usage time
        public string MostActiveDay { get; set; } = string.Empty; // Monday, Tuesday, etc.
        public string MostActiveTimeSlot { get; set; } = string.Empty; // Morning, Afternoon, Evening, Night

        // Favorite vehicle
        public int? FavoriteVehicleId { get; set; }
        public string FavoriteVehicleName { get; set; } = string.Empty;
        public int FavoriteVehicleBookingCount { get; set; }
    }

    /// <summary>
    /// Individual booking in personal history
    /// </summary>
    public class PersonalBookingHistory
    {
        public int BookingId { get; set; }
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string VehicleLicensePlate { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal DurationHours { get; set; }

        public int? DistanceTraveled { get; set; }
        public decimal? FuelLevelStart { get; set; }
        public decimal? FuelLevelEnd { get; set; }

        public string Status { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public decimal? TotalCost { get; set; }

        public bool HasCheckIn { get; set; }
        public bool HasCheckOut { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }

        public bool HasDamageReport { get; set; }
        public bool HasMaintenanceIssue { get; set; }
    }

    /// <summary>
    /// Usage summary for a specific vehicle
    /// </summary>
    public class VehicleUsageSummary
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;

        public decimal OwnershipPercentage { get; set; }
        public decimal InvestmentAmount { get; set; }

        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalDistance { get; set; }
        public decimal TotalCost { get; set; }

        public decimal UsagePercentage { get; set; }
        public decimal UsageVsOwnershipDelta { get; set; }
        public string UsagePattern { get; set; } = string.Empty; // Balanced, Overutilized, Underutilized

        public DateTime FirstBooking { get; set; }
        public DateTime LastBooking { get; set; }
    }

    /// <summary>
    /// Usage statistics for a time period
    /// </summary>
    public class UsagePeriodStatistics
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string PeriodLabel { get; set; } = string.Empty; // "Jan 2024", "Week 1", etc.

        public int BookingCount { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalDistance { get; set; }
        public decimal AverageDuration { get; set; }

        // Day of week distribution
        public Dictionary<string, int> BookingsByDayOfWeek { get; set; } = new();
    }

    /// <summary>
    /// Pagination information
    /// </summary>
    public class PaginationInfo
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
    }

    /// <summary>
    /// Response containing group usage summary
    /// </summary>
    public class GroupUsageSummaryResponse
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;

        // Overall group statistics
        public GroupStatistics GroupStats { get; set; } = new();

        // Co-owner breakdown
        public List<CoOwnerGroupUsage> CoOwners { get; set; } = new();

        // Usage distribution analysis
        public UsageDistributionAnalysis Distribution { get; set; } = new();

        // Time period breakdown
        public List<GroupPeriodUsage> PeriodBreakdown { get; set; } = new();

        // Popular time slots
        public List<PopularTimeSlot> PopularTimeSlots { get; set; } = new();

        // Vehicle utilization metrics
        public VehicleUtilizationMetrics Utilization { get; set; } = new();

        public DateTime AnalysisStartDate { get; set; }
        public DateTime AnalysisEndDate { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Overall group statistics
    /// </summary>
    public class GroupStatistics
    {
        public int TotalCoOwners { get; set; }
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public int ActiveCoOwners { get; set; } // Co-owners with at least 1 booking

        public decimal TotalHoursUsed { get; set; }
        public decimal TotalDistanceTraveled { get; set; }
        public decimal AverageHoursPerBooking { get; set; }
        public decimal AverageDistancePerTrip { get; set; }

        public decimal TotalFundBalance { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }

        // Efficiency metrics
        public decimal UtilizationRate { get; set; } // Percentage of time vehicle was in use
        public decimal AverageBookingsPerCoOwner { get; set; }
        public decimal FairnessScore { get; set; } // 0-100, how fair the usage distribution is
    }

    /// <summary>
    /// Co-owner contribution to group usage
    /// </summary>
    public class CoOwnerGroupUsage
    {
        public int CoOwnerId { get; set; }
        public int UserId { get; set; }
        public string CoOwnerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public decimal OwnershipPercentage { get; set; }
        public decimal InvestmentAmount { get; set; }

        public int BookingCount { get; set; }
        public decimal BookingPercentage { get; set; }
        public decimal TotalHours { get; set; }
        public decimal HoursPercentage { get; set; }
        public decimal TotalDistance { get; set; }
        public decimal DistancePercentage { get; set; }

        public decimal UsageVsOwnershipDelta { get; set; }
        public string UsagePattern { get; set; } = string.Empty;

        public decimal ContributionToFund { get; set; }
        public decimal ContributionPercentage { get; set; }

        public bool IsActive { get; set; } // Has at least 1 booking
        public DateTime? LastBookingDate { get; set; }
    }

    /// <summary>
    /// Usage distribution analysis
    /// </summary>
    public class UsageDistributionAnalysis
    {
        // Distribution by co-owner
        public decimal DistributionVariance { get; set; }
        public string DistributionPattern { get; set; } = string.Empty; // Equal, Dominated, Varied

        public CoOwnerGroupUsage? MostActiveCoOwner { get; set; }
        public CoOwnerGroupUsage? LeastActiveCoOwner { get; set; }

        // Distribution by day of week
        public Dictionary<string, decimal> HoursByDayOfWeek { get; set; } = new();

        // Distribution by time of day
        public Dictionary<string, int> BookingsByTimeOfDay { get; set; } = new();

        // Distribution by purpose
        public Dictionary<string, int> BookingsByPurpose { get; set; } = new();
    }

    /// <summary>
    /// Group usage for a time period
    /// </summary>
    public class GroupPeriodUsage
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string PeriodLabel { get; set; } = string.Empty;

        public int TotalBookings { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalDistance { get; set; }

        public int ActiveCoOwners { get; set; }
        public decimal UtilizationRate { get; set; }

        // Top performer of the period
        public string TopCoOwnerName { get; set; } = string.Empty;
        public decimal TopCoOwnerUsageHours { get; set; }
    }

    /// <summary>
    /// Popular time slot for bookings
    /// </summary>
    public class PopularTimeSlot
    {
        public string TimeSlot { get; set; } = string.Empty; // "Monday Morning", "Wednesday Evening", etc.
        public int BookingCount { get; set; }
        public decimal PercentageOfTotal { get; set; }
        public decimal AverageDuration { get; set; }
    }

    /// <summary>
    /// Vehicle utilization metrics
    /// </summary>
    public class VehicleUtilizationMetrics
    {
        public decimal TotalAvailableHours { get; set; }
        public decimal TotalBookedHours { get; set; }
        public decimal UtilizationPercentage { get; set; }

        public decimal AverageBookingsPerDay { get; set; }
        public decimal AverageBookingsPerWeek { get; set; }
        public decimal AverageBookingsPerMonth { get; set; }

        public int PeakUsageDay { get; set; } // Day of month with most bookings
        public string PeakUsageDayName { get; set; } = string.Empty;

        public int IdleDays { get; set; } // Days with no bookings
        public decimal IdlePercentage { get; set; }
    }

    #endregion

    #region Validators

    /// <summary>
    /// Validator for GetPersonalUsageHistoryRequest
    /// </summary>
    public class GetPersonalUsageHistoryRequestValidator : AbstractValidator<GetPersonalUsageHistoryRequest>
    {
        public GetPersonalUsageHistoryRequestValidator()
        {
            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate)
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                .WithMessage("End date must be after start date");

            RuleFor(x => x.Status)
                .Must(x => string.IsNullOrEmpty(x) || new[] { "All", "Completed", "Cancelled", "Pending" }.Contains(x))
                .WithMessage("Status must be one of: All, Completed, Cancelled, Pending");

            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("Page number must be greater than 0");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Page size must be between 1 and 100");

            RuleFor(x => x.SortBy)
                .Must(x => string.IsNullOrEmpty(x) || new[] { "StartTime", "EndTime", "DurationHours", "Distance" }.Contains(x))
                .WithMessage("SortBy must be one of: StartTime, EndTime, DurationHours, Distance");

            RuleFor(x => x.SortOrder)
                .Must(x => string.IsNullOrEmpty(x) || new[] { "asc", "desc" }.Contains(x.ToLower()))
                .WithMessage("SortOrder must be 'asc' or 'desc'");
        }
    }

    /// <summary>
    /// Validator for GetGroupUsageSummaryRequest
    /// </summary>
    public class GetGroupUsageSummaryRequestValidator : AbstractValidator<GetGroupUsageSummaryRequest>
    {
        public GetGroupUsageSummaryRequestValidator()
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
        }
    }

    #endregion
}
