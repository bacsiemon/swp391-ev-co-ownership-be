using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace EvCoOwnership.Repositories.DTOs.OwnershipDTOs
{
    /// <summary>
    /// Request to propose a change to ownership percentages
    /// Requires approval from all affected co-owners
    /// </summary>
    public class ProposeOwnershipChangeRequest
    {
        /// <summary>
        /// Vehicle ID for which ownership is being changed
        /// </summary>
        [Required(ErrorMessage = "VEHICLE_ID_REQUIRED")]
        public int VehicleId { get; set; }

        /// <summary>
        /// Reason for the ownership change
        /// </summary>
        [Required(ErrorMessage = "REASON_REQUIRED")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "REASON_MUST_BE_BETWEEN_10_AND_1000_CHARACTERS")]
        public string Reason { get; set; } = null!;

        /// <summary>
        /// Proposed ownership changes for each co-owner
        /// Total must equal 100%
        /// </summary>
        [Required(ErrorMessage = "OWNERSHIP_CHANGES_REQUIRED")]
        [MinLength(1, ErrorMessage = "AT_LEAST_ONE_OWNERSHIP_CHANGE_REQUIRED")]
        public List<ProposedOwnershipChange> ProposedChanges { get; set; } = new List<ProposedOwnershipChange>();
    }

    /// <summary>
    /// Proposed ownership change for a single co-owner
    /// </summary>
    public class ProposedOwnershipChange
    {
        /// <summary>
        /// Co-owner ID (from VehicleCoOwner table)
        /// </summary>
        [Required(ErrorMessage = "CO_OWNER_ID_REQUIRED")]
        public int CoOwnerId { get; set; }

        /// <summary>
        /// Proposed new ownership percentage
        /// </summary>
        [Required(ErrorMessage = "PROPOSED_PERCENTAGE_REQUIRED")]
        [Range(0.01, 100, ErrorMessage = "PROPOSED_PERCENTAGE_MUST_BE_BETWEEN_0_01_AND_100")]
        public decimal ProposedPercentage { get; set; }

        /// <summary>
        /// Proposed new investment amount (optional, for financial adjustments)
        /// </summary>
        [Range(0, 10000000000, ErrorMessage = "PROPOSED_INVESTMENT_MUST_BE_BETWEEN_0_AND_10B_VND")]
        public decimal? ProposedInvestment { get; set; }
    }

    /// <summary>
    /// Request to approve or reject an ownership change
    /// </summary>
    public class ApproveOwnershipChangeRequest
    {
        /// <summary>
        /// Approve (true) or reject (false) the ownership change
        /// </summary>
        [Required(ErrorMessage = "APPROVAL_DECISION_REQUIRED")]
        public bool Approve { get; set; }

        /// <summary>
        /// Optional comments explaining the decision
        /// </summary>
        [StringLength(500, ErrorMessage = "COMMENTS_MAX_500_CHARACTERS")]
        public string? Comments { get; set; }
    }

    /// <summary>
    /// Response containing ownership change request details
    /// </summary>
    public class OwnershipChangeRequestResponse
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = null!;
        public string LicensePlate { get; set; } = null!;
        public int ProposedByUserId { get; set; }
        public string ProposerName { get; set; } = null!;
        public string ProposerEmail { get; set; } = null!;
        public string Reason { get; set; } = null!;
        public string Status { get; set; } = null!;
        public int RequiredApprovals { get; set; }
        public int CurrentApprovals { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? FinalizedAt { get; set; }
        public List<OwnershipChangeDetailResponse> ProposedChanges { get; set; } = new List<OwnershipChangeDetailResponse>();
        public List<ApprovalResponse> Approvals { get; set; } = new List<ApprovalResponse>();
    }

    /// <summary>
    /// Detail of a proposed ownership change for a specific co-owner
    /// </summary>
    public class OwnershipChangeDetailResponse
    {
        public int Id { get; set; }
        public int CoOwnerId { get; set; }
        public int UserId { get; set; }
        public string CoOwnerName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public decimal CurrentPercentage { get; set; }
        public decimal ProposedPercentage { get; set; }
        public decimal PercentageChange { get; set; }
        public decimal CurrentInvestment { get; set; }
        public decimal ProposedInvestment { get; set; }
        public decimal InvestmentChange { get; set; }
    }

    /// <summary>
    /// Approval response from a co-owner
    /// </summary>
    public class ApprovalResponse
    {
        public int Id { get; set; }
        public int CoOwnerId { get; set; }
        public int UserId { get; set; }
        public string CoOwnerName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string ApprovalStatus { get; set; } = null!;
        public string? Comments { get; set; }
        public DateTime? RespondedAt { get; set; }
    }

    /// <summary>
    /// Statistics for ownership change requests
    /// </summary>
    public class OwnershipChangeStatisticsResponse
    {
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }
        public int CancelledRequests { get; set; }
        public int ExpiredRequests { get; set; }
        public decimal AverageApprovalTime { get; set; }
        public DateTime? LastRequestCreated { get; set; }
        public DateTime? StatisticsGeneratedAt { get; set; }
    }

    /// <summary>
    /// Notification data for ownership change events
    /// </summary>
    public class OwnershipChangeNotificationData
    {
        public int OwnershipChangeRequestId { get; set; }
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = null!;
        public string LicensePlate { get; set; } = null!;
        public string ProposerName { get; set; } = null!;
        public string Reason { get; set; } = null!;
        public decimal? YourCurrentPercentage { get; set; }
        public decimal? YourProposedPercentage { get; set; }
    }

    /// <summary>
    /// Validator for ProposeOwnershipChangeRequest
    /// </summary>
    public class ProposeOwnershipChangeRequestValidator : AbstractValidator<ProposeOwnershipChangeRequest>
    {
        public ProposeOwnershipChangeRequestValidator()
        {
            RuleFor(x => x.VehicleId)
                .GreaterThan(0)
                .WithMessage("VEHICLE_ID_MUST_BE_GREATER_THAN_0");

            RuleFor(x => x.Reason)
                .NotEmpty()
                .WithMessage("REASON_REQUIRED")
                .Length(10, 1000)
                .WithMessage("REASON_MUST_BE_BETWEEN_10_AND_1000_CHARACTERS");

            RuleFor(x => x.ProposedChanges)
                .NotEmpty()
                .WithMessage("AT_LEAST_ONE_OWNERSHIP_CHANGE_REQUIRED")
                .Must(changes => changes.Sum(c => c.ProposedPercentage) == 100)
                .WithMessage("TOTAL_PROPOSED_OWNERSHIP_MUST_EQUAL_100_PERCENT")
                .Must(changes => changes.All(c => c.ProposedPercentage > 0))
                .WithMessage("ALL_PROPOSED_PERCENTAGES_MUST_BE_GREATER_THAN_0");

            RuleForEach(x => x.ProposedChanges)
                .SetValidator(new ProposedOwnershipChangeValidator());
        }
    }

    /// <summary>
    /// Validator for ProposedOwnershipChange
    /// </summary>
    public class ProposedOwnershipChangeValidator : AbstractValidator<ProposedOwnershipChange>
    {
        public ProposedOwnershipChangeValidator()
        {
            RuleFor(x => x.CoOwnerId)
                .GreaterThan(0)
                .WithMessage("CO_OWNER_ID_MUST_BE_GREATER_THAN_0");

            RuleFor(x => x.ProposedPercentage)
                .InclusiveBetween(0.01m, 100m)
                .WithMessage("PROPOSED_PERCENTAGE_MUST_BE_BETWEEN_0_01_AND_100");

            RuleFor(x => x.ProposedInvestment)
                .InclusiveBetween(0m, 10000000000m)
                .When(x => x.ProposedInvestment.HasValue)
                .WithMessage("PROPOSED_INVESTMENT_MUST_BE_BETWEEN_0_AND_10B_VND");
        }
    }

    /// <summary>
    /// Validator for ApproveOwnershipChangeRequest
    /// </summary>
    public class ApproveOwnershipChangeRequestValidator : AbstractValidator<ApproveOwnershipChangeRequest>
    {
        public ApproveOwnershipChangeRequestValidator()
        {
            RuleFor(x => x.Comments)
                .MaximumLength(500)
                .WithMessage("COMMENTS_MAX_500_CHARACTERS");
        }
    }
}
