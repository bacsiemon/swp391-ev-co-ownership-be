using EvCoOwnership.Repositories.Enums;
using FluentValidation;

namespace EvCoOwnership.Repositories.DTOs.BookingDTOs
{
    #region Modification DTOs

    /// <summary>
    /// Enhanced request to modify an existing booking
    /// </summary>
    public class ModifyBookingRequest
    {
        /// <summary>
        /// New start time (optional)
        /// </summary>
        public DateTime? NewStartTime { get; set; }

        /// <summary>
        /// New end time (optional)
        /// </summary>
        public DateTime? NewEndTime { get; set; }

        /// <summary>
        /// New purpose (optional)
        /// </summary>
        public string? NewPurpose { get; set; }

        /// <summary>
        /// Reason for modification
        /// </summary>
        public string ModificationReason { get; set; } = string.Empty;

        /// <summary>
        /// Whether to skip conflict check (for emergency changes)
        /// </summary>
        public bool SkipConflictCheck { get; set; } = false;

        /// <summary>
        /// Whether to notify affected co-owners
        /// </summary>
        public bool NotifyAffectedCoOwners { get; set; } = true;

        /// <summary>
        /// Request approval from co-owners if modification causes conflicts
        /// </summary>
        public bool RequestApprovalIfConflict { get; set; } = true;
    }

    /// <summary>
    /// Response for booking modification
    /// </summary>
    public class ModifyBookingResponse
    {
        public int BookingId { get; set; }
        public ModificationStatus Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public BookingModificationSummary? OriginalBooking { get; set; }
        public BookingModificationSummary? ModifiedBooking { get; set; }
        public ModificationImpactAnalysis? ImpactAnalysis { get; set; }
        public List<string> RequiredApprovals { get; set; } = new();
        public List<string> NotifiedCoOwners { get; set; } = new();
        public DateTime ModifiedAt { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<AlternativeSlotSuggestion>? SuggestedAlternatives { get; set; }
    }

    /// <summary>
    /// Summary of booking details for comparison
    /// </summary>
    public class BookingModificationSummary
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public int DurationHours { get; set; }
        public EBookingStatus Status { get; set; }
    }

    /// <summary>
    /// Impact analysis of the modification
    /// </summary>
    public class ModificationImpactAnalysis
    {
        public bool HasTimeChange { get; set; }
        public bool HasConflicts { get; set; }
        public int ConflictCount { get; set; }
        public List<ConflictingBookingInfo>? ConflictingBookings { get; set; }
        public int TimeDeltaHours { get; set; }
        public bool RequiresCoOwnerApproval { get; set; }
        public decimal? EstimatedCostChange { get; set; }
        public string ImpactSummary { get; set; } = string.Empty;
    }

    /// <summary>
    /// Enhanced cancel booking request
    /// </summary>
    public class CancelBookingRequest
    {
        /// <summary>
        /// Reason for cancellation (required)
        /// </summary>
        public string CancellationReason { get; set; } = string.Empty;

        /// <summary>
        /// Cancellation type
        /// </summary>
        public CancellationType CancellationType { get; set; } = CancellationType.UserInitiated;

        /// <summary>
        /// Whether to request reschedule instead of full cancel
        /// </summary>
        public bool RequestReschedule { get; set; } = false;

        /// <summary>
        /// Preferred reschedule time (if requesting reschedule)
        /// </summary>
        public DateTime? PreferredRescheduleStart { get; set; }

        /// <summary>
        /// Preferred reschedule end time
        /// </summary>
        public DateTime? PreferredRescheduleEnd { get; set; }

        /// <summary>
        /// Accept cancellation penalty/fee
        /// </summary>
        public bool AcceptCancellationFee { get; set; } = true;
    }

    /// <summary>
    /// Response for cancellation
    /// </summary>
    public class CancelBookingResponse
    {
        public int BookingId { get; set; }
        public CancellationStatus Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CancelledAt { get; set; }
        public CancellationPolicyInfo PolicyInfo { get; set; } = new();
        public BookingModificationSummary? CancelledBooking { get; set; }
        public List<AlternativeSlotSuggestion>? RescheduleOptions { get; set; }
        public List<string> NotifiedCoOwners { get; set; } = new();
        public RefundInfo? RefundInfo { get; set; }
    }

    /// <summary>
    /// Cancellation policy information
    /// </summary>
    public class CancellationPolicyInfo
    {
        public bool IsCancellationAllowed { get; set; }
        public decimal CancellationFee { get; set; }
        public int HoursUntilBooking { get; set; }
        public string PolicyRule { get; set; } = string.Empty;
        public bool IsWithinGracePeriod { get; set; }
        public string GracePeriodInfo { get; set; } = string.Empty;
    }

    /// <summary>
    /// Refund information
    /// </summary>
    public class RefundInfo
    {
        public bool IsRefundable { get; set; }
        public decimal RefundAmount { get; set; }
        public decimal RefundPercentage { get; set; }
        public string RefundReason { get; set; } = string.Empty;
        public DateTime? EstimatedRefundDate { get; set; }
    }

    /// <summary>
    /// Get modification history request
    /// </summary>
    public class GetModificationHistoryRequest
    {
        public int? BookingId { get; set; }
        public int? UserId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ModificationStatus? FilterByStatus { get; set; }
    }

    /// <summary>
    /// Modification history response
    /// </summary>
    public class ModificationHistoryResponse
    {
        public int TotalModifications { get; set; }
        public int TotalCancellations { get; set; }
        public List<ModificationHistoryEntry> History { get; set; } = new();
    }

    /// <summary>
    /// Single modification history entry
    /// </summary>
    public class ModificationHistoryEntry
    {
        public int HistoryId { get; set; }
        public int BookingId { get; set; }
        public string ModificationType { get; set; } = string.Empty;
        public string ModifiedBy { get; set; } = string.Empty;
        public DateTime ModifiedAt { get; set; }
        public string Reason { get; set; } = string.Empty;
        public BookingModificationSummary? BeforeChange { get; set; }
        public BookingModificationSummary? AfterChange { get; set; }
        public ModificationStatus Status { get; set; }
        public bool RequiredApproval { get; set; }
        public List<string>? ApprovedBy { get; set; }
    }

    /// <summary>
    /// Validate modification feasibility request
    /// </summary>
    public class ValidateModificationRequest
    {
        public int BookingId { get; set; }
        public DateTime? NewStartTime { get; set; }
        public DateTime? NewEndTime { get; set; }
    }

    /// <summary>
    /// Validation result
    /// </summary>
    public class ModificationValidationResult
    {
        public bool IsValid { get; set; }
        public bool HasConflicts { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public ModificationImpactAnalysis? ImpactAnalysis { get; set; }
        public List<AlternativeSlotSuggestion>? AlternativeSuggestions { get; set; }
        public string Recommendation { get; set; } = string.Empty;
    }

    #endregion

    #region Enums

    /// <summary>
    /// Status of booking modification
    /// </summary>
    public enum ModificationStatus
    {
        Success = 0,                // Modification applied successfully
        PendingApproval = 1,        // Waiting for co-owner approval
        Rejected = 2,               // Modification rejected
        Failed = 3,                 // Technical failure
        ConflictDetected = 4        // Conflict with other bookings
    }

    /// <summary>
    /// Type of cancellation
    /// </summary>
    public enum CancellationType
    {
        UserInitiated = 0,          // User voluntarily cancelled
        SystemCancelled = 1,        // System cancelled (e.g., no-show)
        EmergencyCancellation = 2,  // Emergency cancellation
        VehicleUnavailable = 3,     // Vehicle not available
        MaintenanceRequired = 4     // Vehicle needs maintenance
    }

    /// <summary>
    /// Status of cancellation
    /// </summary>
    public enum CancellationStatus
    {
        Cancelled = 0,              // Successfully cancelled
        CancelledWithFee = 1,       // Cancelled with penalty
        CancelledWithRefund = 2,    // Cancelled with refund
        Rescheduled = 3,            // Rescheduled instead of cancelled
        Failed = 4                  // Cancellation failed
    }

    #endregion

    #region Validators

    public class ModifyBookingRequestValidator : AbstractValidator<ModifyBookingRequest>
    {
        public ModifyBookingRequestValidator()
        {
            RuleFor(x => x.ModificationReason)
                .NotEmpty()
                .WithMessage("Modification reason is required")
                .MaximumLength(500)
                .WithMessage("Modification reason cannot exceed 500 characters");

            RuleFor(x => x.NewEndTime)
                .GreaterThan(x => x.NewStartTime)
                .When(x => x.NewStartTime.HasValue && x.NewEndTime.HasValue)
                .WithMessage("New end time must be after new start time");

            RuleFor(x => x.NewStartTime)
                .GreaterThanOrEqualTo(DateTime.UtcNow.AddHours(-1))
                .When(x => x.NewStartTime.HasValue)
                .WithMessage("Cannot modify booking to a past time");

            RuleFor(x => x)
                .Must(x => x.NewStartTime.HasValue || x.NewEndTime.HasValue || !string.IsNullOrWhiteSpace(x.NewPurpose))
                .WithMessage("At least one field must be modified (time or purpose)");
        }
    }

    public class CancelBookingRequestValidator : AbstractValidator<CancelBookingRequest>
    {
        public CancelBookingRequestValidator()
        {
            RuleFor(x => x.CancellationReason)
                .NotEmpty()
                .WithMessage("Cancellation reason is required")
                .MinimumLength(10)
                .WithMessage("Please provide a detailed cancellation reason (at least 10 characters)")
                .MaximumLength(500)
                .WithMessage("Cancellation reason cannot exceed 500 characters");

            RuleFor(x => x.CancellationType)
                .IsInEnum()
                .WithMessage("Invalid cancellation type");

            RuleFor(x => x.PreferredRescheduleEnd)
                .GreaterThan(x => x.PreferredRescheduleStart)
                .When(x => x.RequestReschedule && x.PreferredRescheduleStart.HasValue && x.PreferredRescheduleEnd.HasValue)
                .WithMessage("Reschedule end time must be after start time");

            RuleFor(x => x.PreferredRescheduleStart)
                .GreaterThan(DateTime.UtcNow)
                .When(x => x.RequestReschedule && x.PreferredRescheduleStart.HasValue)
                .WithMessage("Reschedule time must be in the future");

            RuleFor(x => x)
                .Must(x => x.PreferredRescheduleStart.HasValue && x.PreferredRescheduleEnd.HasValue)
                .When(x => x.RequestReschedule)
                .WithMessage("Both reschedule start and end times are required when requesting reschedule");
        }
    }

    public class ValidateModificationRequestValidator : AbstractValidator<ValidateModificationRequest>
    {
        public ValidateModificationRequestValidator()
        {
            RuleFor(x => x.BookingId)
                .GreaterThan(0)
                .WithMessage("Valid booking ID is required");

            RuleFor(x => x.NewEndTime)
                .GreaterThan(x => x.NewStartTime)
                .When(x => x.NewStartTime.HasValue && x.NewEndTime.HasValue)
                .WithMessage("New end time must be after new start time");
        }
    }

    #endregion
}
