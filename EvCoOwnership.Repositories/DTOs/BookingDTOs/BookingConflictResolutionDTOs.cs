using EvCoOwnership.Repositories.Enums;
using FluentValidation;

namespace EvCoOwnership.Repositories.DTOs.BookingDTOs
{
    #region Conflict Resolution DTOs

    /// <summary>
    /// Request to approve/reject a booking with conflict resolution
    /// </summary>
    public class ResolveBookingConflictRequest
    {
        /// <summary>
        /// Whether to approve the conflicting booking
        /// </summary>
        public bool IsApproved { get; set; }

        /// <summary>
        /// Resolution type
        /// </summary>
        public ConflictResolutionType ResolutionType { get; set; } = ConflictResolutionType.SimpleApproval;

        /// <summary>
        /// Reason for rejection (required if IsApproved = false)
        /// </summary>
        public string? RejectionReason { get; set; }

        /// <summary>
        /// Priority justification if you want to claim priority over the request
        /// </summary>
        public string? PriorityJustification { get; set; }

        /// <summary>
        /// Counter-offer: Suggest alternative time slot to requester
        /// </summary>
        public DateTime? CounterOfferStartTime { get; set; }

        /// <summary>
        /// Counter-offer: Suggested end time
        /// </summary>
        public DateTime? CounterOfferEndTime { get; set; }

        /// <summary>
        /// Notes for the requester or other co-owners
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Whether to apply ownership weight in resolution (higher % = higher priority)
        /// </summary>
        public bool UseOwnershipWeighting { get; set; } = true;

        /// <summary>
        /// Whether to auto-negotiate based on usage patterns and fairness
        /// </summary>
        public bool EnableAutoNegotiation { get; set; } = false;
    }

    /// <summary>
    /// Response for conflict resolution action
    /// </summary>
    public class BookingConflictResolutionResponse
    {
        /// <summary>
        /// Booking/request ID that was resolved
        /// </summary>
        public int BookingId { get; set; }

        /// <summary>
        /// Resolution outcome
        /// </summary>
        public ConflictResolutionOutcome Outcome { get; set; }

        /// <summary>
        /// Final status after resolution
        /// </summary>
        public EBookingStatus FinalStatus { get; set; }

        /// <summary>
        /// Who resolved the conflict
        /// </summary>
        public string ResolvedBy { get; set; } = string.Empty;

        /// <summary>
        /// When was it resolved
        /// </summary>
        public DateTime ResolvedAt { get; set; }

        /// <summary>
        /// Explanation of the resolution
        /// </summary>
        public string ResolutionExplanation { get; set; } = string.Empty;

        /// <summary>
        /// If counter-offer was made
        /// </summary>
        public CounterOfferInfo? CounterOffer { get; set; }

        /// <summary>
        /// All co-owners involved in this conflict
        /// </summary>
        public List<ConflictStakeholder> Stakeholders { get; set; } = new();

        /// <summary>
        /// Approval status from each stakeholder
        /// </summary>
        public ConflictApprovalStatus ApprovalStatus { get; set; } = new();

        /// <summary>
        /// Auto-resolution details if applied
        /// </summary>
        public AutoResolutionInfo? AutoResolution { get; set; }

        /// <summary>
        /// Next actions recommended
        /// </summary>
        public List<string> RecommendedActions { get; set; } = new();
    }

    /// <summary>
    /// Counter-offer information
    /// </summary>
    public class CounterOfferInfo
    {
        public DateTime SuggestedStartTime { get; set; }
        public DateTime SuggestedEndTime { get; set; }
        public string Reason { get; set; } = string.Empty;
        public bool IsRequesterAccepted { get; set; } = false;
        public DateTime? AcceptedAt { get; set; }
    }

    /// <summary>
    /// Stakeholder in a booking conflict
    /// </summary>
    public class ConflictStakeholder
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal OwnershipPercentage { get; set; }
        public int UsageHoursThisMonth { get; set; }
        public bool HasApproved { get; set; }
        public bool HasRejected { get; set; }
        public DateTime? ResponseDate { get; set; }
        public string? ResponseNotes { get; set; }
        public int PriorityWeight { get; set; } // Calculated weight based on ownership + usage fairness
    }

    /// <summary>
    /// Overall approval status for a conflict
    /// </summary>
    public class ConflictApprovalStatus
    {
        public int TotalStakeholders { get; set; }
        public int ApprovalsReceived { get; set; }
        public int RejectionsReceived { get; set; }
        public int PendingResponses { get; set; }
        public decimal ApprovalPercentage { get; set; }
        public decimal WeightedApprovalPercentage { get; set; } // Based on ownership %
        public bool IsFullyApproved { get; set; }
        public bool IsRejected { get; set; }
        public bool RequiresMoreApprovals { get; set; }
        public List<string> PendingFrom { get; set; } = new();
    }

    /// <summary>
    /// Auto-resolution information
    /// </summary>
    public class AutoResolutionInfo
    {
        public bool WasAutoResolved { get; set; }
        public AutoResolutionReason Reason { get; set; }
        public string Explanation { get; set; } = string.Empty;
        public decimal RequesterPriorityScore { get; set; }
        public decimal ConflictingOwnerPriorityScore { get; set; }
        public string WinnerName { get; set; } = string.Empty;
        public List<string> FactorsConsidered { get; set; } = new();
    }

    /// <summary>
    /// Request to get pending conflicts requiring resolution
    /// </summary>
    public class GetPendingConflictsRequest
    {
        /// <summary>
        /// Vehicle ID to filter by
        /// </summary>
        public int? VehicleId { get; set; }

        /// <summary>
        /// Only show conflicts where this user is involved
        /// </summary>
        public bool OnlyMyConflicts { get; set; } = false;

        /// <summary>
        /// Filter by priority level
        /// </summary>
        public BookingPriority? MinimumPriority { get; set; }

        /// <summary>
        /// Include auto-resolvable conflicts
        /// </summary>
        public bool IncludeAutoResolvable { get; set; } = true;
    }

    /// <summary>
    /// Response with pending conflicts
    /// </summary>
    public class PendingConflictsResponse
    {
        public int TotalConflicts { get; set; }
        public int RequiringMyAction { get; set; }
        public int AutoResolvable { get; set; }
        public List<ConflictSummary> Conflicts { get; set; } = new();
        public DateTime OldestConflictDate { get; set; }
        public List<string> ActionItems { get; set; } = new();
    }

    /// <summary>
    /// Summary of a single conflict
    /// </summary>
    public class ConflictSummary
    {
        public int BookingId { get; set; }
        public string RequesterName { get; set; } = string.Empty;
        public DateTime RequestedStartTime { get; set; }
        public DateTime RequestedEndTime { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public BookingPriority Priority { get; set; }
        public List<DetailedConflictingBookingInfo> ConflictsWith { get; set; } = new();
        public DateTime RequestedAt { get; set; }
        public int DaysPending { get; set; }
        public ConflictApprovalStatus ApprovalStatus { get; set; } = new();
        public bool CanAutoResolve { get; set; }
        public AutoResolutionPreview? AutoResolutionPreview { get; set; }
    }

    /// <summary>
    /// Detailed conflicting booking information for conflict resolution
    /// </summary>
    public class DetailedConflictingBookingInfo
    {
        public int BookingId { get; set; }
        public string CoOwnerName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public EBookingStatus Status { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public decimal OverlapHours { get; set; }
        public decimal CoOwnerOwnershipPercentage { get; set; }
        public bool HasResponded { get; set; }
    }

    /// <summary>
    /// Preview of what would happen with auto-resolution
    /// </summary>
    public class AutoResolutionPreview
    {
        public ConflictResolutionOutcome PredictedOutcome { get; set; }
        public string WinnerName { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public decimal Confidence { get; set; } // 0-1
        public List<string> Factors { get; set; } = new();
    }

    /// <summary>
    /// Booking conflict analytics
    /// </summary>
    public class BookingConflictAnalyticsResponse
    {
        public int TotalConflictsResolved { get; set; }
        public int TotalConflictsPending { get; set; }
        public decimal AverageResolutionTimeHours { get; set; }
        public decimal ApprovalRate { get; set; }
        public decimal RejectionRate { get; set; }
        public decimal AutoResolutionRate { get; set; }
        public List<CoOwnerConflictStats> StatsByCoOwner { get; set; } = new();
        public List<ConflictPattern> CommonPatterns { get; set; } = new();
        public List<ConflictResolutionRecommendation> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// Conflict statistics by co-owner
    /// </summary>
    public class CoOwnerConflictStats
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ConflictsInitiated { get; set; }
        public int ConflictsReceived { get; set; }
        public int ApprovalsGiven { get; set; }
        public int RejectionsGiven { get; set; }
        public decimal ApprovalRateAsResponder { get; set; }
        public decimal SuccessRateAsRequester { get; set; }
        public decimal AverageResponseTimeHours { get; set; }
    }

    /// <summary>
    /// Common conflict pattern
    /// </summary>
    public class ConflictPattern
    {
        public string Pattern { get; set; } = string.Empty;
        public int Occurrences { get; set; }
        public string Recommendation { get; set; } = string.Empty;
    }

    /// <summary>
    /// Recommendation for conflict resolution
    /// </summary>
    public class ConflictResolutionRecommendation
    {
        public string Recommendation { get; set; } = string.Empty;
        public string Rationale { get; set; } = string.Empty;
        public ConflictResolutionType SuggestedApproach { get; set; }
    }

    #endregion

    #region Enums

    /// <summary>
    /// Type of conflict resolution
    /// </summary>
    public enum ConflictResolutionType
    {
        SimpleApproval = 0,         // Just approve or reject
        CounterOffer = 1,           // Reject with alternative time suggestion
        PriorityOverride = 2,       // Claim priority based on ownership/usage
        AutoNegotiation = 3,        // Let system auto-negotiate based on fairness
        ConsensusRequired = 4       // All stakeholders must approve
    }

    /// <summary>
    /// Outcome of conflict resolution
    /// </summary>
    public enum ConflictResolutionOutcome
    {
        Approved = 0,               // Request approved, conflicting booking cancelled
        Rejected = 1,               // Request rejected, conflicting booking stays
        CounterOfferMade = 2,       // Alternative time suggested
        AutoResolved = 3,           // System auto-resolved based on rules
        AwaitingMoreApprovals = 4,  // Need more co-owner approvals
        Negotiating = 5             // In negotiation phase
    }

    /// <summary>
    /// Reason for auto-resolution
    /// </summary>
    public enum AutoResolutionReason
    {
        OwnershipWeight = 0,        // Higher ownership % wins
        UsageFairness = 1,          // Less usage this month wins
        PriorityLevel = 2,          // Higher priority wins
        FirstComeFirstServed = 3,   // Earlier booking wins
        ConsensusReached = 4        // All stakeholders approved
    }

    #endregion

    #region Validators

    public class ResolveBookingConflictRequestValidator : AbstractValidator<ResolveBookingConflictRequest>
    {
        public ResolveBookingConflictRequestValidator()
        {
            RuleFor(x => x.RejectionReason)
                .NotEmpty()
                .When(x => !x.IsApproved)
                .WithMessage("Rejection reason is required when rejecting a booking");

            RuleFor(x => x.RejectionReason)
                .MaximumLength(500)
                .WithMessage("Rejection reason cannot exceed 500 characters");

            RuleFor(x => x.PriorityJustification)
                .MaximumLength(500)
                .WithMessage("Priority justification cannot exceed 500 characters");

            RuleFor(x => x.Notes)
                .MaximumLength(1000)
                .WithMessage("Notes cannot exceed 1000 characters");

            RuleFor(x => x.CounterOfferEndTime)
                .GreaterThan(x => x.CounterOfferStartTime)
                .When(x => x.CounterOfferStartTime.HasValue && x.CounterOfferEndTime.HasValue)
                .WithMessage("Counter-offer end time must be after start time");

            RuleFor(x => x.CounterOfferStartTime)
                .GreaterThan(DateTime.UtcNow)
                .When(x => x.CounterOfferStartTime.HasValue)
                .WithMessage("Counter-offer start time must be in the future");

            RuleFor(x => x.ResolutionType)
                .IsInEnum()
                .WithMessage("Invalid resolution type");

            RuleFor(x => x)
                .Must(x => x.CounterOfferStartTime.HasValue && x.CounterOfferEndTime.HasValue)
                .When(x => x.ResolutionType == ConflictResolutionType.CounterOffer)
                .WithMessage("Counter-offer times are required when using CounterOffer resolution type");
        }
    }

    public class GetPendingConflictsRequestValidator : AbstractValidator<GetPendingConflictsRequest>
    {
        public GetPendingConflictsRequestValidator()
        {
            RuleFor(x => x.VehicleId)
                .GreaterThan(0)
                .When(x => x.VehicleId.HasValue)
                .WithMessage("Vehicle ID must be greater than 0");

            RuleFor(x => x.MinimumPriority)
                .IsInEnum()
                .When(x => x.MinimumPriority.HasValue)
                .WithMessage("Invalid priority level");
        }
    }

    #endregion
}
