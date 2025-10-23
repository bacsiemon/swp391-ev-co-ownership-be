using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Repositories.DTOs.VehicleDTOs
{
    /// <summary>
    /// Request to get vehicle availability schedule
    /// </summary>
    public class VehicleAvailabilityScheduleRequest
    {
        /// <summary>
        /// Start date to check availability
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End date to check availability
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Optional: Filter by booking status
        /// </summary>
        public EBookingStatus? StatusFilter { get; set; }
    }

    /// <summary>
    /// Time slot showing vehicle availability
    /// </summary>
    public class VehicleTimeSlot
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsAvailable { get; set; }
        public int? BookingId { get; set; }
        public string? BookedBy { get; set; }
        public string? Purpose { get; set; }
        public EBookingStatus? BookingStatus { get; set; }
    }

    /// <summary>
    /// Vehicle availability schedule response
    /// </summary>
    public class VehicleAvailabilityScheduleResponse
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public EVehicleStatus Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<VehicleTimeSlot> BookedSlots { get; set; } = new List<VehicleTimeSlot>();
        public List<DateTime> AvailableDays { get; set; } = new List<DateTime>();
        public VehicleUtilizationStats UtilizationStats { get; set; } = new VehicleUtilizationStats();
    }

    /// <summary>
    /// Vehicle utilization statistics
    /// </summary>
    public class VehicleUtilizationStats
    {
        public int TotalDaysInPeriod { get; set; }
        public int TotalBookedHours { get; set; }
        public int TotalAvailableHours { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public int TotalBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int PendingBookings { get; set; }
        public decimal AverageBookingDuration { get; set; }
    }

    /// <summary>
    /// Request to find available time slots
    /// </summary>
    public class FindAvailableTimeSlotsRequest
    {
        /// <summary>
        /// Start date to search
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End date to search
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Minimum duration required (in hours)
        /// </summary>
        public int MinimumDurationHours { get; set; } = 1;

        /// <summary>
        /// Only show full day slots (8+ hours)
        /// </summary>
        public bool FullDayOnly { get; set; } = false;
    }

    /// <summary>
    /// Available time slot suggestion
    /// </summary>
    public class AvailableTimeSlot
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int DurationHours { get; set; }
        public bool IsFullDay { get; set; }
        public string Recommendation { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response with available time slots
    /// </summary>
    public class AvailableTimeSlotsResponse
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MinimumDurationHours { get; set; }
        public List<AvailableTimeSlot> AvailableSlots { get; set; } = new List<AvailableTimeSlot>();
        public int TotalSlotsFound { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Multi-vehicle utilization comparison
    /// </summary>
    public class VehicleUtilizationComparison
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public decimal UtilizationPercentage { get; set; }
        public int TotalBookings { get; set; }
        public int TotalBookedHours { get; set; }
        public string MostActiveDay { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response for comparing vehicle utilization
    /// </summary>
    public class VehicleUtilizationComparisonResponse
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<VehicleUtilizationComparison> Vehicles { get; set; } = new List<VehicleUtilizationComparison>();
        public VehicleUtilizationComparison? MostUtilizedVehicle { get; set; }
        public VehicleUtilizationComparison? LeastUtilizedVehicle { get; set; }
        public decimal AverageUtilization { get; set; }
    }
}
