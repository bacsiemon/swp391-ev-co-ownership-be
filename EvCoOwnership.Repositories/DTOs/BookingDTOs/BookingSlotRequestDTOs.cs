using EvCoOwnership.Repositories.Enums;
using FluentValidation;

namespace EvCoOwnership.Repositories.DTOs.BookingDTOs
{
    #region Request DTOs

    /// <summary>
    /// Request to reserve a booking slot with optional alternate times
    /// </summary>
    public class RequestBookingSlotRequest
    {
        /// <summary>
        /// Preferred start time for the booking
        /// </summary>
        public DateTime PreferredStartTime { get; set; }

        /// <summary>
        /// Preferred end time for the booking
        /// </summary>
        public DateTime PreferredEndTime { get; set; }

        /// <summary>
        /// Purpose/reason for booking
        /// </summary>
        public string Purpose { get; set; } = string.Empty;

        /// <summary>
        /// Priority level (Low, Medium, High)
        /// </summary>
        public BookingPriority Priority { get; set; } = BookingPriority.Medium;

        /// <summary>
        /// Whether this booking is flexible (can accept alternative times)
        /// </summary>
        public bool IsFlexible { get; set; } = false;

        /// <summary>
        /// Optional: Alternative time slots if preferred slot is unavailable
        /// </summary>
        public List<AlternativeTimeSlot>? AlternativeSlots { get; set; }

        /// <summary>
        /// Estimated distance to travel (km) - for planning purposes
        /// </summary>
        public int? EstimatedDistance { get; set; }

        /// <summary>
        /// Usage type for this booking
        /// </summary>
        public EUsageType? UsageType { get; set; }

        /// <summary>
        /// Whether to auto-confirm if slot is available
        /// </summary>
        public bool AutoConfirmIfAvailable { get; set; } = true;
    }

    /// <summary>
    /// Alternative time slot option
    /// </summary>
    public class AlternativeTimeSlot
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int PreferenceRank { get; set; } // 1 = most preferred alternative
    }

    /// <summary>
    /// Request to respond to a booking slot request
    /// </summary>
    public class RespondToSlotRequestRequest
    {
        /// <summary>
        /// Whether to approve the slot request
        /// </summary>
        public bool IsApproved { get; set; }

        /// <summary>
        /// Reason for rejection (required if IsApproved = false)
        /// </summary>
        public string? RejectionReason { get; set; }

        /// <summary>
        /// Optional: Suggest alternative time slot
        /// </summary>
        public DateTime? SuggestedStartTime { get; set; }

        /// <summary>
        /// Optional: Suggested end time
        /// </summary>
        public DateTime? SuggestedEndTime { get; set; }

        /// <summary>
        /// Optional: Notes for the requester
        /// </summary>
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Request to cancel a pending slot request
    /// </summary>
    public class CancelSlotRequestRequest
    {
        /// <summary>
        /// Reason for cancellation
        /// </summary>
        public string Reason { get; set; } = string.Empty;
    }

    #endregion

    #region Response DTOs

    /// <summary>
    /// Response for booking slot request
    /// </summary>
    public class BookingSlotRequestResponse
    {
        public int RequestId { get; set; }
        public int BookingId { get; set; }
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public int RequesterId { get; set; }
        public string RequesterName { get; set; } = string.Empty;
        public DateTime PreferredStartTime { get; set; }
        public DateTime PreferredEndTime { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public BookingPriority Priority { get; set; }
        public SlotRequestStatus Status { get; set; }
        public bool IsFlexible { get; set; }
        public int? EstimatedDistance { get; set; }
        public EUsageType? UsageType { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? ProcessedBy { get; set; }
        public SlotAvailabilityStatus AvailabilityStatus { get; set; }
        public List<ConflictingBookingInfo>? ConflictingBookings { get; set; }
        public List<AlternativeSlotSuggestion>? AlternativeSuggestions { get; set; }
        public string? AutoConfirmationMessage { get; set; }
        public BookingSlotRequestMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Conflicting booking information
    /// </summary>
    public class ConflictingBookingInfo
    {
        public int BookingId { get; set; }
        public string CoOwnerName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public EBookingStatus Status { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public decimal OverlapHours { get; set; }
    }

    /// <summary>
    /// Alternative slot suggestion
    /// </summary>
    public class AlternativeSlotSuggestion
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal DurationHours { get; set; }
        public bool IsAvailable { get; set; }
        public string Reason { get; set; } = string.Empty;
        public decimal ConflictProbability { get; set; }
        public int RecommendationScore { get; set; } // 0-100
    }

    /// <summary>
    /// Request metadata
    /// </summary>
    public class BookingSlotRequestMetadata
    {
        public int TotalAlternativesProvided { get; set; }
        public int ProcessingTimeSeconds { get; set; }
        public bool RequiresCoOwnerApproval { get; set; }
        public List<string> ApprovalPendingFrom { get; set; } = new();
        public string SystemRecommendation { get; set; } = string.Empty;
    }

    /// <summary>
    /// Pending slot requests response
    /// </summary>
    public class PendingSlotRequestsResponse
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public List<BookingSlotRequestResponse> PendingRequests { get; set; } = new();
        public int TotalPendingCount { get; set; }
        public DateTime OldestRequestDate { get; set; }
    }

    /// <summary>
    /// Slot request analytics
    /// </summary>
    public class SlotRequestAnalytics
    {
        public int TotalRequests { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int AutoConfirmedCount { get; set; }
        public int CancelledCount { get; set; }
        public decimal AverageProcessingTimeHours { get; set; }
        public decimal ApprovalRate { get; set; }
        public List<PopularTimeSlot> MostRequestedTimeSlots { get; set; } = new();
        public List<CoOwnerRequestStats> RequestsByCoOwner { get; set; } = new();
    }

    /// <summary>
    /// Popular time slot
    /// </summary>
    public class PopularTimeSlot
    {
        public DayOfWeek DayOfWeek { get; set; }
        public int HourOfDay { get; set; }
        public int RequestCount { get; set; }
        public decimal ApprovalRate { get; set; }
    }

    /// <summary>
    /// Co-owner request statistics
    /// </summary>
    public class CoOwnerRequestStats
    {
        public int CoOwnerId { get; set; }
        public string CoOwnerName { get; set; } = string.Empty;
        public int TotalRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }
        public decimal ApprovalRate { get; set; }
    }

    #endregion

    #region Enums

    /// <summary>
    /// Booking priority levels
    /// </summary>
    public enum BookingPriority
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Urgent = 3
    }

    /// <summary>
    /// Slot request status
    /// </summary>
    public enum SlotRequestStatus
    {
        Pending = 0,           // Awaiting approval
        AutoConfirmed = 1,     // Automatically confirmed (no conflicts)
        Approved = 2,          // Manually approved
        Rejected = 3,          // Rejected by co-owner/system
        Cancelled = 4,         // Cancelled by requester
        Expired = 5,           // Request expired (past requested time)
        ConflictResolved = 6   // Conflict resolved with alternative slot
    }

    /// <summary>
    /// Slot availability status
    /// </summary>
    public enum SlotAvailabilityStatus
    {
        Available = 0,              // Fully available
        PartiallyAvailable = 1,     // Some overlap with existing bookings
        Unavailable = 2,            // Fully booked
        RequiresApproval = 3        // Available but requires co-owner approval
    }

    #endregion

    #region Validators

    /// <summary>
    /// Validator for RequestBookingSlotRequest
    /// </summary>
    public class RequestBookingSlotValidator : AbstractValidator<RequestBookingSlotRequest>
    {
        public RequestBookingSlotValidator()
        {
            RuleFor(x => x.PreferredStartTime)
                .NotEmpty().WithMessage("Preferred start time is required")
                .GreaterThan(DateTime.Now).WithMessage("Start time must be in the future");

            RuleFor(x => x.PreferredEndTime)
                .NotEmpty().WithMessage("Preferred end time is required")
                .GreaterThan(x => x.PreferredStartTime)
                .WithMessage("End time must be after start time");

            RuleFor(x => x.Purpose)
                .NotEmpty().WithMessage("Purpose is required")
                .MaximumLength(500).WithMessage("Purpose cannot exceed 500 characters");

            RuleFor(x => x.Priority)
                .IsInEnum().WithMessage("Invalid priority level");

            RuleFor(x => x.EstimatedDistance)
                .GreaterThan(0).When(x => x.EstimatedDistance.HasValue)
                .WithMessage("Estimated distance must be greater than 0");

            RuleFor(x => x.AlternativeSlots)
                .Must(slots => slots == null || slots.Count <= 5)
                .WithMessage("Maximum 5 alternative slots allowed");

            When(x => x.AlternativeSlots != null && x.AlternativeSlots.Any(), () =>
            {
                RuleForEach(x => x.AlternativeSlots)
                    .Must(slot => slot.EndTime > slot.StartTime)
                    .WithMessage("Alternative slot end time must be after start time");
            });
        }
    }

    /// <summary>
    /// Validator for RespondToSlotRequestRequest
    /// </summary>
    public class RespondToSlotRequestValidator : AbstractValidator<RespondToSlotRequestRequest>
    {
        public RespondToSlotRequestValidator()
        {
            RuleFor(x => x.RejectionReason)
                .NotEmpty()
                .When(x => !x.IsApproved)
                .WithMessage("Rejection reason is required when rejecting a request");

            RuleFor(x => x.RejectionReason)
                .MaximumLength(500)
                .When(x => !string.IsNullOrEmpty(x.RejectionReason))
                .WithMessage("Rejection reason cannot exceed 500 characters");

            RuleFor(x => x.SuggestedEndTime)
                .GreaterThan(x => x.SuggestedStartTime)
                .When(x => x.SuggestedStartTime.HasValue && x.SuggestedEndTime.HasValue)
                .WithMessage("Suggested end time must be after suggested start time");

            RuleFor(x => x.Notes)
                .MaximumLength(1000)
                .When(x => !string.IsNullOrEmpty(x.Notes))
                .WithMessage("Notes cannot exceed 1000 characters");
        }
    }

    /// <summary>
    /// Validator for CancelSlotRequestRequest
    /// </summary>
    public class CancelSlotRequestValidator : AbstractValidator<CancelSlotRequestRequest>
    {
        public CancelSlotRequestValidator()
        {
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Cancellation reason is required")
                .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");
        }
    }

    #endregion
}
