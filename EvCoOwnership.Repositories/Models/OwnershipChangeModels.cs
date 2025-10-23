using System;
using System.Collections.Generic;
using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Repositories.Models
{
    /// <summary>
    /// Represents a proposed change to ownership percentages for a vehicle
    /// Requires approval from all affected co-owners (group consensus)
    /// </summary>
    public partial class OwnershipChangeRequest
    {
        public int Id { get; set; }

        /// <summary>
        /// Vehicle for which ownership is being changed
        /// </summary>
        public int VehicleId { get; set; }

        /// <summary>
        /// User who proposed this change
        /// </summary>
        public int ProposedByUserId { get; set; }

        /// <summary>
        /// Reason for the ownership change
        /// </summary>
        public string Reason { get; set; } = null!;

        /// <summary>
        /// Status of the ownership change request
        /// </summary>
        public EOwnershipChangeStatus? StatusEnum { get; set; }

        /// <summary>
        /// Date when the request was created
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Date when the request was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Date when the request was finalized (approved/rejected)
        /// </summary>
        public DateTime? FinalizedAt { get; set; }

        /// <summary>
        /// Total number of approvals required
        /// </summary>
        public int RequiredApprovals { get; set; }

        /// <summary>
        /// Current number of approvals received
        /// </summary>
        public int CurrentApprovals { get; set; }

        /// <summary>
        /// Navigation property to vehicle
        /// </summary>
        public virtual Vehicle Vehicle { get; set; } = null!;

        /// <summary>
        /// Navigation property to user who proposed the change
        /// </summary>
        public virtual User ProposedByUser { get; set; } = null!;

        /// <summary>
        /// Collection of proposed ownership changes for each co-owner
        /// </summary>
        public virtual ICollection<OwnershipChangeDetail> OwnershipChangeDetails { get; set; } = new List<OwnershipChangeDetail>();

        /// <summary>
        /// Collection of approval responses from co-owners
        /// </summary>
        public virtual ICollection<OwnershipChangeApproval> OwnershipChangeApprovals { get; set; } = new List<OwnershipChangeApproval>();
    }

    /// <summary>
    /// Represents the proposed new ownership percentage for a specific co-owner
    /// </summary>
    public partial class OwnershipChangeDetail
    {
        public int Id { get; set; }

        /// <summary>
        /// Reference to the ownership change request
        /// </summary>
        public int OwnershipChangeRequestId { get; set; }

        /// <summary>
        /// Co-owner affected by this change
        /// </summary>
        public int CoOwnerId { get; set; }

        /// <summary>
        /// Current ownership percentage before the change
        /// </summary>
        public decimal CurrentPercentage { get; set; }

        /// <summary>
        /// Proposed new ownership percentage
        /// </summary>
        public decimal ProposedPercentage { get; set; }

        /// <summary>
        /// Current investment amount
        /// </summary>
        public decimal CurrentInvestment { get; set; }

        /// <summary>
        /// Proposed new investment amount (if applicable)
        /// </summary>
        public decimal ProposedInvestment { get; set; }

        /// <summary>
        /// Date when this detail was created
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Navigation property to ownership change request
        /// </summary>
        public virtual OwnershipChangeRequest OwnershipChangeRequest { get; set; } = null!;

        /// <summary>
        /// Navigation property to co-owner
        /// </summary>
        public virtual CoOwner CoOwner { get; set; } = null!;
    }

    /// <summary>
    /// Represents a co-owner's approval or rejection of an ownership change request
    /// </summary>
    public partial class OwnershipChangeApproval
    {
        public int Id { get; set; }

        /// <summary>
        /// Reference to the ownership change request
        /// </summary>
        public int OwnershipChangeRequestId { get; set; }

        /// <summary>
        /// Co-owner who is approving/rejecting
        /// </summary>
        public int CoOwnerId { get; set; }

        /// <summary>
        /// User ID of the co-owner
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Approval decision (Approved, Rejected, Pending)
        /// </summary>
        public EApprovalStatus? ApprovalStatusEnum { get; set; }

        /// <summary>
        /// Optional comments from the co-owner
        /// </summary>
        public string? Comments { get; set; }

        /// <summary>
        /// Date when approval was given/rejected
        /// </summary>
        public DateTime? RespondedAt { get; set; }

        /// <summary>
        /// Date when this record was created
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Navigation property to ownership change request
        /// </summary>
        public virtual OwnershipChangeRequest OwnershipChangeRequest { get; set; } = null!;

        /// <summary>
        /// Navigation property to co-owner
        /// </summary>
        public virtual CoOwner CoOwner { get; set; } = null!;

        /// <summary>
        /// Navigation property to user
        /// </summary>
        public virtual User User { get; set; } = null!;
    }
}
