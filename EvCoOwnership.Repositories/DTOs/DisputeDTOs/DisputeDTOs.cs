using FluentValidation;
using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Repositories.DTOs.DisputeDTOs
{
    #region Request DTOs

    /// <summary>
    /// Request to raise a booking dispute
    /// </summary>
    public class RaiseBookingDisputeRequest
    {
        /// <summary>
        /// Booking ID related to the dispute
        /// </summary>
        public int BookingId { get; set; }

        /// <summary>
        /// Vehicle ID (for validation)
        /// </summary>
        public int VehicleId { get; set; }

        /// <summary>
        /// Title/subject of the dispute
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of the issue
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Dispute priority: Low, Medium, High, Critical (default: Medium)
        /// </summary>
        public string Priority { get; set; } = "Medium";

        /// <summary>
        /// IDs of users being disputed against (optional)
        /// </summary>
        public List<int> RespondentUserIds { get; set; } = new();

        /// <summary>
        /// Evidence URLs (images, documents)
        /// </summary>
        public List<string> EvidenceUrls { get; set; } = new();

        /// <summary>
        /// Requested resolution/outcome
        /// </summary>
        public string RequestedResolution { get; set; } = string.Empty;

        /// <summary>
        /// Specific booking dispute category
        /// </summary>
        public string Category { get; set; } = string.Empty; // Unauthorized, Cancellation, Damage, NoShow, etc.
    }

    /// <summary>
    /// Request to raise a cost sharing dispute
    /// </summary>
    public class RaiseCostSharingDisputeRequest
    {
        /// <summary>
        /// Vehicle ID related to the cost sharing
        /// </summary>
        public int VehicleId { get; set; }

        /// <summary>
        /// Related payment ID (optional)
        /// </summary>
        public int? PaymentId { get; set; }

        /// <summary>
        /// Related maintenance cost ID (optional)
        /// </summary>
        public int? MaintenanceCostId { get; set; }

        /// <summary>
        /// Related fund usage ID (optional)
        /// </summary>
        public int? FundUsageId { get; set; }

        /// <summary>
        /// Title/subject of the dispute
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of the cost dispute
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Disputed amount
        /// </summary>
        public decimal DisputedAmount { get; set; }

        /// <summary>
        /// Expected/fair amount
        /// </summary>
        public decimal? ExpectedAmount { get; set; }

        /// <summary>
        /// Dispute priority (default: Medium)
        /// </summary>
        public string Priority { get; set; } = "Medium";

        /// <summary>
        /// IDs of users being disputed against
        /// </summary>
        public List<int> RespondentUserIds { get; set; } = new();

        /// <summary>
        /// Evidence URLs (receipts, invoices, etc.)
        /// </summary>
        public List<string> EvidenceUrls { get; set; } = new();

        /// <summary>
        /// Requested resolution
        /// </summary>
        public string RequestedResolution { get; set; } = string.Empty;

        /// <summary>
        /// Cost dispute category
        /// </summary>
        public string Category { get; set; } = string.Empty; // Overcharge, UnfairSplit, Unauthorized, InvalidCost, etc.
    }

    /// <summary>
    /// Request to raise a group decision dispute
    /// </summary>
    public class RaiseGroupDecisionDisputeRequest
    {
        /// <summary>
        /// Vehicle ID related to the group decision
        /// </summary>
        public int VehicleId { get; set; }

        /// <summary>
        /// Related fund usage vote ID (optional)
        /// </summary>
        public int? FundUsageVoteId { get; set; }

        /// <summary>
        /// Related vehicle upgrade proposal ID (optional)
        /// </summary>
        public int? VehicleUpgradeProposalId { get; set; }

        /// <summary>
        /// Related ownership change ID (optional)
        /// </summary>
        public int? OwnershipChangeId { get; set; }

        /// <summary>
        /// Title/subject of the dispute
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of the issue with the decision
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Dispute priority (default: Medium)
        /// </summary>
        public string Priority { get; set; } = "Medium";

        /// <summary>
        /// IDs of users being disputed against
        /// </summary>
        public List<int> RespondentUserIds { get; set; } = new();

        /// <summary>
        /// Evidence URLs
        /// </summary>
        public List<string> EvidenceUrls { get; set; } = new();

        /// <summary>
        /// Requested resolution
        /// </summary>
        public string RequestedResolution { get; set; } = string.Empty;

        /// <summary>
        /// Group decision dispute category
        /// </summary>
        public string Category { get; set; } = string.Empty; // VotingIrregularity, UnfairProcess, PolicyViolation, etc.

        /// <summary>
        /// Specific policy or rule allegedly violated
        /// </summary>
        public string? ViolatedPolicy { get; set; }
    }

    /// <summary>
    /// Request to respond to a dispute
    /// </summary>
    public class RespondToDisputeRequest
    {
        /// <summary>
        /// Response message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Counter-evidence URLs
        /// </summary>
        public List<string> EvidenceUrls { get; set; } = new();

        /// <summary>
        /// Whether the respondent agrees with the dispute
        /// </summary>
        public bool AgreesWithDispute { get; set; }

        /// <summary>
        /// Proposed solution from respondent
        /// </summary>
        public string? ProposedSolution { get; set; }
    }

    /// <summary>
    /// Request to update dispute status (Admin/Mediator)
    /// </summary>
    public class UpdateDisputeStatusRequest
    {
        /// <summary>
        /// New status: Open, UnderReview, InMediation, Resolved, Rejected, Withdrawn, Escalated
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Resolution notes/decision
        /// </summary>
        public string? ResolutionNotes { get; set; }

        /// <summary>
        /// Actions to be taken (if resolved)
        /// </summary>
        public string? ActionsRequired { get; set; }
    }

    /// <summary>
    /// Request to get disputes with filters
    /// </summary>
    public class GetDisputesRequest
    {
        /// <summary>
        /// Filter by vehicle ID
        /// </summary>
        public int? VehicleId { get; set; }

        /// <summary>
        /// Filter by dispute type: Booking, CostSharing, GroupDecision, VehicleDamage, OwnershipChange, Other
        /// </summary>
        public string? DisputeType { get; set; }

        /// <summary>
        /// Filter by status: Open, UnderReview, InMediation, Resolved, Rejected, Withdrawn, Escalated
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Filter by priority: Low, Medium, High, Critical
        /// </summary>
        public string? Priority { get; set; }

        /// <summary>
        /// Filter disputes where user is initiator
        /// </summary>
        public bool? IsInitiator { get; set; }

        /// <summary>
        /// Filter disputes where user is respondent
        /// </summary>
        public bool? IsRespondent { get; set; }

        /// <summary>
        /// Start date for filtering
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// End date for filtering
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Page number (default: 1)
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Page size (default: 20)
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Sort by: CreatedDate, UpdatedDate, Priority (default: CreatedDate)
        /// </summary>
        public string SortBy { get; set; } = "CreatedDate";

        /// <summary>
        /// Sort order: asc or desc (default: desc)
        /// </summary>
        public string SortOrder { get; set; } = "desc";
    }

    #endregion

    #region Response DTOs

    /// <summary>
    /// Dispute details response
    /// </summary>
    public class DisputeResponse
    {
        public int DisputeId { get; set; }
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string VehicleLicensePlate { get; set; } = string.Empty;

        public string DisputeType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RequestedResolution { get; set; } = string.Empty;

        // Initiator information
        public int InitiatorUserId { get; set; }
        public string InitiatorName { get; set; } = string.Empty;
        public string InitiatorEmail { get; set; } = string.Empty;

        // Respondents
        public List<DisputeParticipant> Respondents { get; set; } = new();

        // Related entities
        public int? RelatedBookingId { get; set; }
        public int? RelatedPaymentId { get; set; }
        public int? RelatedMaintenanceCostId { get; set; }
        public int? RelatedFundUsageId { get; set; }
        public int? RelatedFundUsageVoteId { get; set; }
        public int? RelatedVehicleUpgradeProposalId { get; set; }
        public int? RelatedOwnershipChangeId { get; set; }

        // Evidence
        public List<string> EvidenceUrls { get; set; } = new();

        // Cost-specific fields
        public decimal? DisputedAmount { get; set; }
        public decimal? ExpectedAmount { get; set; }

        // Group decision-specific fields
        public string? ViolatedPolicy { get; set; }

        // Resolution
        public string? ResolutionNotes { get; set; }
        public string? ActionsRequired { get; set; }
        public int? ResolvedByUserId { get; set; }
        public string? ResolvedByName { get; set; }
        public DateTime? ResolvedAt { get; set; }

        // Responses
        public List<DisputeResponseItem> Responses { get; set; } = new();

        // Timeline
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int DaysOpen { get; set; }
    }

    /// <summary>
    /// Dispute participant information
    /// </summary>
    public class DisputeParticipant
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool HasResponded { get; set; }
        public DateTime? RespondedAt { get; set; }
    }

    /// <summary>
    /// Response to a dispute
    /// </summary>
    public class DisputeResponseItem
    {
        public int ResponseId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public List<string> EvidenceUrls { get; set; } = new();
        public bool AgreesWithDispute { get; set; }
        public string? ProposedSolution { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// List of disputes with pagination
    /// </summary>
    public class DisputeListResponse
    {
        public List<DisputeSummary> Disputes { get; set; } = new();
        public DisputeStatistics Statistics { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
    }

    /// <summary>
    /// Summary information for a dispute
    /// </summary>
    public class DisputeSummary
    {
        public int DisputeId { get; set; }
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        
        public string DisputeType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        
        public string Title { get; set; } = string.Empty;
        public string InitiatorName { get; set; } = string.Empty;
        public int RespondentCount { get; set; }
        public int ResponseCount { get; set; }
        
        public decimal? DisputedAmount { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int DaysOpen { get; set; }
    }

    /// <summary>
    /// Dispute statistics
    /// </summary>
    public class DisputeStatistics
    {
        public int TotalDisputes { get; set; }
        public int OpenDisputes { get; set; }
        public int UnderReviewDisputes { get; set; }
        public int InMediationDisputes { get; set; }
        public int ResolvedDisputes { get; set; }
        public int RejectedDisputes { get; set; }

        public int BookingDisputes { get; set; }
        public int CostSharingDisputes { get; set; }
        public int GroupDecisionDisputes { get; set; }

        public int HighPriorityDisputes { get; set; }
        public int CriticalPriorityDisputes { get; set; }

        public decimal AverageResolutionDays { get; set; }
        public decimal ResolutionRate { get; set; } // Percentage resolved
    }

    /// <summary>
    /// Pagination information (reuse from existing)
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

    #endregion

    #region Validators

    /// <summary>
    /// Validator for RaiseBookingDisputeRequest
    /// </summary>
    public class RaiseBookingDisputeRequestValidator : AbstractValidator<RaiseBookingDisputeRequest>
    {
        public RaiseBookingDisputeRequestValidator()
        {
            RuleFor(x => x.BookingId)
                .GreaterThan(0)
                .WithMessage("BookingId must be greater than 0");

            RuleFor(x => x.VehicleId)
                .GreaterThan(0)
                .WithMessage("VehicleId must be greater than 0");

            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Title is required")
                .MaximumLength(200)
                .WithMessage("Title must not exceed 200 characters");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Description is required")
                .MinimumLength(20)
                .WithMessage("Description must be at least 20 characters")
                .MaximumLength(2000)
                .WithMessage("Description must not exceed 2000 characters");

            RuleFor(x => x.Priority)
                .Must(x => new[] { "Low", "Medium", "High", "Critical" }.Contains(x))
                .WithMessage("Priority must be one of: Low, Medium, High, Critical");

            RuleFor(x => x.RequestedResolution)
                .NotEmpty()
                .WithMessage("Requested resolution is required")
                .MaximumLength(1000)
                .WithMessage("Requested resolution must not exceed 1000 characters");

            RuleFor(x => x.Category)
                .NotEmpty()
                .WithMessage("Category is required");
        }
    }

    /// <summary>
    /// Validator for RaiseCostSharingDisputeRequest
    /// </summary>
    public class RaiseCostSharingDisputeRequestValidator : AbstractValidator<RaiseCostSharingDisputeRequest>
    {
        public RaiseCostSharingDisputeRequestValidator()
        {
            RuleFor(x => x.VehicleId)
                .GreaterThan(0)
                .WithMessage("VehicleId must be greater than 0");

            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Title is required")
                .MaximumLength(200)
                .WithMessage("Title must not exceed 200 characters");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Description is required")
                .MinimumLength(20)
                .WithMessage("Description must be at least 20 characters")
                .MaximumLength(2000)
                .WithMessage("Description must not exceed 2000 characters");

            RuleFor(x => x.DisputedAmount)
                .GreaterThan(0)
                .WithMessage("Disputed amount must be greater than 0");

            RuleFor(x => x.ExpectedAmount)
                .GreaterThan(0)
                .When(x => x.ExpectedAmount.HasValue)
                .WithMessage("Expected amount must be greater than 0");

            RuleFor(x => x.Priority)
                .Must(x => new[] { "Low", "Medium", "High", "Critical" }.Contains(x))
                .WithMessage("Priority must be one of: Low, Medium, High, Critical");

            RuleFor(x => x.RequestedResolution)
                .NotEmpty()
                .WithMessage("Requested resolution is required");

            RuleFor(x => x.Category)
                .NotEmpty()
                .WithMessage("Category is required");

            RuleFor(x => x)
                .Must(x => x.PaymentId.HasValue || x.MaintenanceCostId.HasValue || x.FundUsageId.HasValue)
                .WithMessage("At least one related entity (PaymentId, MaintenanceCostId, or FundUsageId) must be provided");
        }
    }

    /// <summary>
    /// Validator for RaiseGroupDecisionDisputeRequest
    /// </summary>
    public class RaiseGroupDecisionDisputeRequestValidator : AbstractValidator<RaiseGroupDecisionDisputeRequest>
    {
        public RaiseGroupDecisionDisputeRequestValidator()
        {
            RuleFor(x => x.VehicleId)
                .GreaterThan(0)
                .WithMessage("VehicleId must be greater than 0");

            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Title is required")
                .MaximumLength(200)
                .WithMessage("Title must not exceed 200 characters");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Description is required")
                .MinimumLength(20)
                .WithMessage("Description must be at least 20 characters")
                .MaximumLength(2000)
                .WithMessage("Description must not exceed 2000 characters");

            RuleFor(x => x.Priority)
                .Must(x => new[] { "Low", "Medium", "High", "Critical" }.Contains(x))
                .WithMessage("Priority must be one of: Low, Medium, High, Critical");

            RuleFor(x => x.RequestedResolution)
                .NotEmpty()
                .WithMessage("Requested resolution is required");

            RuleFor(x => x.Category)
                .NotEmpty()
                .WithMessage("Category is required");

            RuleFor(x => x)
                .Must(x => x.FundUsageVoteId.HasValue || x.VehicleUpgradeProposalId.HasValue || x.OwnershipChangeId.HasValue)
                .WithMessage("At least one related decision entity must be provided");
        }
    }

    /// <summary>
    /// Validator for RespondToDisputeRequest
    /// </summary>
    public class RespondToDisputeRequestValidator : AbstractValidator<RespondToDisputeRequest>
    {
        public RespondToDisputeRequestValidator()
        {
            RuleFor(x => x.Message)
                .NotEmpty()
                .WithMessage("Response message is required")
                .MinimumLength(10)
                .WithMessage("Response message must be at least 10 characters")
                .MaximumLength(2000)
                .WithMessage("Response message must not exceed 2000 characters");
        }
    }

    /// <summary>
    /// Validator for UpdateDisputeStatusRequest
    /// </summary>
    public class UpdateDisputeStatusRequestValidator : AbstractValidator<UpdateDisputeStatusRequest>
    {
        public UpdateDisputeStatusRequestValidator()
        {
            RuleFor(x => x.Status)
                .NotEmpty()
                .WithMessage("Status is required")
                .Must(x => new[] { "Open", "UnderReview", "InMediation", "Resolved", "Rejected", "Withdrawn", "Escalated" }.Contains(x))
                .WithMessage("Status must be one of: Open, UnderReview, InMediation, Resolved, Rejected, Withdrawn, Escalated");

            RuleFor(x => x.ResolutionNotes)
                .NotEmpty()
                .When(x => x.Status == "Resolved" || x.Status == "Rejected")
                .WithMessage("Resolution notes are required when status is Resolved or Rejected");
        }
    }

    /// <summary>
    /// Validator for GetDisputesRequest
    /// </summary>
    public class GetDisputesRequestValidator : AbstractValidator<GetDisputesRequest>
    {
        public GetDisputesRequestValidator()
        {
            RuleFor(x => x.DisputeType)
                .Must(x => string.IsNullOrEmpty(x) || new[] { "Booking", "CostSharing", "GroupDecision", "VehicleDamage", "OwnershipChange", "Other" }.Contains(x))
                .WithMessage("DisputeType must be one of: Booking, CostSharing, GroupDecision, VehicleDamage, OwnershipChange, Other");

            RuleFor(x => x.Status)
                .Must(x => string.IsNullOrEmpty(x) || new[] { "Open", "UnderReview", "InMediation", "Resolved", "Rejected", "Withdrawn", "Escalated" }.Contains(x))
                .WithMessage("Status must be one of: Open, UnderReview, InMediation, Resolved, Rejected, Withdrawn, Escalated");

            RuleFor(x => x.Priority)
                .Must(x => string.IsNullOrEmpty(x) || new[] { "Low", "Medium", "High", "Critical" }.Contains(x))
                .WithMessage("Priority must be one of: Low, Medium, High, Critical");

            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("Page number must be greater than 0");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Page size must be between 1 and 100");

            RuleFor(x => x.SortBy)
                .Must(x => new[] { "CreatedDate", "UpdatedDate", "Priority" }.Contains(x))
                .WithMessage("SortBy must be one of: CreatedDate, UpdatedDate, Priority");

            RuleFor(x => x.SortOrder)
                .Must(x => new[] { "asc", "desc" }.Contains(x.ToLower()))
                .WithMessage("SortOrder must be 'asc' or 'desc'");

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate)
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                .WithMessage("End date must be after start date");
        }
    }

    #endregion
}
