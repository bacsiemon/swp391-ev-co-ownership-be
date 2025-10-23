using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Repositories.DTOs.BookingDTOs
{
    /// <summary>
    /// Request to get booking calendar for a specific time range
    /// </summary>
    public class GetBookingCalendarRequest
    {
        /// <summary>
        /// Start date of the calendar view
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End date of the calendar view
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Optional: Filter by specific vehicle ID
        /// </summary>
        public int? VehicleId { get; set; }

        /// <summary>
        /// Optional: Filter by booking status
        /// </summary>
        public EBookingStatus? Status { get; set; }
    }

    /// <summary>
    /// Calendar event representing a booking
    /// </summary>
    public class BookingCalendarEvent
    {
        public int BookingId { get; set; }
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public int CoOwnerId { get; set; }
        public string CoOwnerName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public EBookingStatus Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public int DurationHours { get; set; }
        public bool IsCurrentUser { get; set; }
    }

    /// <summary>
    /// Response containing booking calendar data
    /// </summary>
    public class BookingCalendarResponse
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<BookingCalendarEvent> Events { get; set; } = new List<BookingCalendarEvent>();
        public int TotalEvents { get; set; }
        public BookingCalendarSummary Summary { get; set; } = new BookingCalendarSummary();
    }

    /// <summary>
    /// Summary statistics for the calendar period
    /// </summary>
    public class BookingCalendarSummary
    {
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int ActiveBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public int TotalVehicles { get; set; }
        public int MyBookings { get; set; }
    }

    /// <summary>
    /// Vehicle availability check for a specific time slot
    /// </summary>
    public class VehicleAvailabilityRequest
    {
        public int VehicleId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    /// <summary>
    /// Response indicating if vehicle is available
    /// </summary>
    public class VehicleAvailabilityResponse
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<BookingCalendarEvent>? ConflictingBookings { get; set; }
    }
}
