using FluentValidation;

namespace EvCoOwnership.Repositories.DTOs.ScheduleDTOs
{
    #region Request DTOs

    /// <summary>
    /// Request to get vehicle schedule
    /// </summary>
    public class GetVehicleScheduleRequest
    {
        /// <summary>
        /// Vehicle ID to get schedule for
        /// </summary>
        public int VehicleId { get; set; }

        /// <summary>
        /// Start date for schedule view
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End date for schedule view
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Filter by booking status (optional)
        /// </summary>
        public string? StatusFilter { get; set; }
    }

    public class GetVehicleScheduleRequestValidator : AbstractValidator<GetVehicleScheduleRequest>
    {
        public GetVehicleScheduleRequestValidator()
        {
            RuleFor(x => x.VehicleId)
                .GreaterThan(0)
                .WithMessage("Vehicle ID is required");

            RuleFor(x => x.StartDate)
                .NotEmpty()
                .WithMessage("Start date is required");

            RuleFor(x => x.EndDate)
                .NotEmpty()
                .GreaterThan(x => x.StartDate)
                .WithMessage("End date must be after start date");

            RuleFor(x => x.EndDate)
                .Must((request, endDate) => (endDate - request.StartDate).TotalDays <= 90)
                .WithMessage("Date range cannot exceed 90 days");
        }
    }

    /// <summary>
    /// Request to check availability for booking
    /// </summary>
    public class CheckAvailabilityRequest
    {
        /// <summary>
        /// Vehicle ID to check availability for
        /// </summary>
        public int VehicleId { get; set; }

        /// <summary>
        /// Proposed start time
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Proposed end time
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Exclude booking ID from conflict check (for updates)
        /// </summary>
        public int? ExcludeBookingId { get; set; }
    }

    public class CheckAvailabilityRequestValidator : AbstractValidator<CheckAvailabilityRequest>
    {
        public CheckAvailabilityRequestValidator()
        {
            RuleFor(x => x.VehicleId)
                .GreaterThan(0)
                .WithMessage("Vehicle ID is required");

            RuleFor(x => x.StartTime)
                .NotEmpty()
                .GreaterThan(DateTime.Now)
                .WithMessage("Start time must be in the future");

            RuleFor(x => x.EndTime)
                .NotEmpty()
                .GreaterThan(x => x.StartTime)
                .WithMessage("End time must be after start time");

            RuleFor(x => x.EndTime)
                .Must((request, endTime) => (endTime - request.StartTime).TotalDays <= 7)
                .WithMessage("Booking duration cannot exceed 7 days");
        }
    }

    /// <summary>
    /// Request to find optimal booking slots
    /// </summary>
    public class FindOptimalSlotsRequest
    {
        /// <summary>
        /// Vehicle ID to find slots for
        /// </summary>
        public int VehicleId { get; set; }

        /// <summary>
        /// Preferred date range start
        /// </summary>
        public DateTime PreferredStartDate { get; set; }

        /// <summary>
        /// Preferred date range end
        /// </summary>
        public DateTime PreferredEndDate { get; set; }

        /// <summary>
        /// Minimum duration required (in hours)
        /// </summary>
        public int MinimumDurationHours { get; set; } = 1;

        /// <summary>
        /// Maximum duration needed (in hours)
        /// </summary>
        public int? MaximumDurationHours { get; set; }

        /// <summary>
        /// Preferred time of day (Morning, Afternoon, Evening, Night)
        /// </summary>
        public string? PreferredTimeOfDay { get; set; }

        /// <summary>
        /// Only show full-day slots (8+ hours)
        /// </summary>
        public bool FullDayOnly { get; set; } = false;
    }

    public class FindOptimalSlotsRequestValidator : AbstractValidator<FindOptimalSlotsRequest>
    {
        public FindOptimalSlotsRequestValidator()
        {
            RuleFor(x => x.VehicleId)
                .GreaterThan(0)
                .WithMessage("Vehicle ID is required");

            RuleFor(x => x.PreferredStartDate)
                .NotEmpty()
                .GreaterThanOrEqualTo(DateTime.Today)
                .WithMessage("Start date cannot be in the past");

            RuleFor(x => x.PreferredEndDate)
                .NotEmpty()
                .GreaterThan(x => x.PreferredStartDate)
                .WithMessage("End date must be after start date");

            RuleFor(x => x.MinimumDurationHours)
                .GreaterThan(0)
                .LessThanOrEqualTo(168) // 7 days
                .WithMessage("Minimum duration must be between 1 and 168 hours");

            RuleFor(x => x.MaximumDurationHours)
                .GreaterThan(x => x.MinimumDurationHours)
                .When(x => x.MaximumDurationHours.HasValue)
                .WithMessage("Maximum duration must be greater than minimum duration");
        }
    }

    #endregion

    #region Response DTOs

    /// <summary>
    /// Vehicle schedule response
    /// </summary>
    public class VehicleScheduleResponse
    {
        /// <summary>
        /// Vehicle information
        /// </summary>
        public VehicleScheduleInfo Vehicle { get; set; } = new();

        /// <summary>
        /// Schedule period
        /// </summary>
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        /// <summary>
        /// All bookings in the period
        /// </summary>
        public List<ScheduleBookingSlot> BookedSlots { get; set; } = new();

        /// <summary>
        /// Available time slots
        /// </summary>
        public List<ScheduleAvailableSlot> AvailableSlots { get; set; } = new();

        /// <summary>
        /// Utilization statistics
        /// </summary>
        public ScheduleUtilizationStats Utilization { get; set; } = new();

        /// <summary>
        /// Generated timestamp
        /// </summary>
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Vehicle information for schedule
    /// </summary>
    public class VehicleScheduleInfo
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = string.Empty;
    }

    /// <summary>
    /// Booked time slot information
    /// </summary>
    public class ScheduleBookingSlot
    {
        public int BookingId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int DurationHours { get; set; }
        public string BookedByUserName { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty; // For UI display
        public bool IsCurrentUser { get; set; }
    }

    /// <summary>
    /// Available time slot information
    /// </summary>
    public class ScheduleAvailableSlot
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int DurationHours { get; set; }
        public string SlotType { get; set; } = string.Empty; // "Short", "Half-Day", "Full-Day", "Extended"
        public string Recommendation { get; set; } = string.Empty;
        public bool IsOptimal { get; set; } // Based on usage patterns
    }

    /// <summary>
    /// Schedule utilization statistics
    /// </summary>
    public class ScheduleUtilizationStats
    {
        public int TotalHoursInPeriod { get; set; }
        public int BookedHours { get; set; }
        public int AvailableHours { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public int TotalBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int PendingBookings { get; set; }
        public decimal AverageBookingDuration { get; set; }
        public List<string> PeakUsageDays { get; set; } = new();
        public List<string> LowUsageDays { get; set; } = new();
    }

    /// <summary>
    /// Availability check response
    /// </summary>
    public class AvailabilityCheckResponse
    {
        /// <summary>
        /// Whether the time slot is available
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// Conflicting bookings if any
        /// </summary>
        public List<ConflictingBooking> Conflicts { get; set; } = new();

        /// <summary>
        /// Alternative suggestions if not available
        /// </summary>
        public List<AlternativeTimeSlot> Alternatives { get; set; } = new();

        /// <summary>
        /// Availability message
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Conflicting booking information
    /// </summary>
    public class ConflictingBooking
    {
        public int BookingId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string BookedByUserName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool CanBeModified { get; set; }
    }

    /// <summary>
    /// Alternative time slot suggestion
    /// </summary>
    public class AlternativeTimeSlot
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int DurationHours { get; set; }
        public string Reason { get; set; } = string.Empty;
        public decimal MatchScore { get; set; } // How well it matches preferences (0-100)
    }

    /// <summary>
    /// Optimal slots response
    /// </summary>
    public class OptimalSlotsResponse
    {
        /// <summary>
        /// Vehicle information
        /// </summary>
        public VehicleScheduleInfo Vehicle { get; set; } = new();

        /// <summary>
        /// Search criteria used
        /// </summary>
        public OptimalSlotsCriteria SearchCriteria { get; set; } = new();

        /// <summary>
        /// Found optimal slots
        /// </summary>
        public List<OptimalTimeSlot> OptimalSlots { get; set; } = new();

        /// <summary>
        /// Additional suggestions
        /// </summary>
        public List<OptimalTimeSlot> AlternativeSlots { get; set; } = new();

        /// <summary>
        /// Search insights
        /// </summary>
        public OptimalSlotsInsights Insights { get; set; } = new();

        /// <summary>
        /// Generated timestamp
        /// </summary>
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Search criteria used for optimal slots
    /// </summary>
    public class OptimalSlotsCriteria
    {
        public DateTime PreferredStartDate { get; set; }
        public DateTime PreferredEndDate { get; set; }
        public int MinimumDurationHours { get; set; }
        public int? MaximumDurationHours { get; set; }
        public string? PreferredTimeOfDay { get; set; }
        public bool FullDayOnly { get; set; }
    }

    /// <summary>
    /// Optimal time slot details
    /// </summary>
    public class OptimalTimeSlot
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int DurationHours { get; set; }
        public string SlotType { get; set; } = string.Empty;
        public decimal OptimalityScore { get; set; } // 0-100
        public string Recommendation { get; set; } = string.Empty;
        public List<string> Benefits { get; set; } = new();
        public bool IsFullDay { get; set; }
        public string TimeOfDay { get; set; } = string.Empty;
    }

    /// <summary>
    /// Insights from optimal slots search
    /// </summary>
    public class OptimalSlotsInsights
    {
        public int TotalSlotsFound { get; set; }
        public int OptimalSlotsCount { get; set; }
        public int AlternativeSlotsCount { get; set; }
        public decimal AverageOptimalityScore { get; set; }
        public string BestTimeOfDay { get; set; } = string.Empty;
        public List<string> RecommendedDays { get; set; } = new();
        public List<string> UsagePatternInsights { get; set; } = new();
        public string OverallRecommendation { get; set; } = string.Empty;
    }

    #endregion
}