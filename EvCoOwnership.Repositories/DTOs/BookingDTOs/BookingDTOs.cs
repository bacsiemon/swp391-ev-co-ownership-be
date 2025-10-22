using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Repositories.DTOs.BookingDTOs
{
    public class CreateBookingRequest
    {
        public int VehicleId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Purpose { get; set; } = string.Empty;
    }

    public class UpdateBookingRequest
    {
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Purpose { get; set; }
    }

    public class ApproveBookingRequest
    {
        public bool IsApproved { get; set; }
        public string? RejectionReason { get; set; }
    }

    public class BookingResponse
    {
        public int Id { get; set; }
        public int CoOwnerId { get; set; }
        public string CoOwnerName { get; set; } = string.Empty;
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public EBookingStatus Status { get; set; }
        public int? ApprovedBy { get; set; }
        public string? ApprovedByName { get; set; }
        public decimal? TotalCost { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class BookingStatisticsResponse
    {
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public int ApprovedBookings { get; set; }
        public int RejectedBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
