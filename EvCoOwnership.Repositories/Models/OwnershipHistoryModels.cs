using System;
using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Repositories.Models
{
    /// <summary>
    /// Represents a historical record of ownership percentage changes
    /// Automatically created when ownership changes are approved and applied
    /// </summary>
    public partial class OwnershipHistory
    {
        public int Id { get; set; }

        /// <summary>
        /// Vehicle for which ownership changed
        /// </summary>
        public int VehicleId { get; set; }

        /// <summary>
        /// Co-owner whose ownership changed
        /// </summary>
        public int CoOwnerId { get; set; }

        /// <summary>
        /// User ID of the co-owner
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Reference to the ownership change request that caused this change
        /// </summary>
        public int? OwnershipChangeRequestId { get; set; }

        /// <summary>
        /// Previous ownership percentage before the change
        /// </summary>
        public decimal PreviousPercentage { get; set; }

        /// <summary>
        /// New ownership percentage after the change
        /// </summary>
        public decimal NewPercentage { get; set; }

        /// <summary>
        /// Change in percentage (NewPercentage - PreviousPercentage)
        /// </summary>
        public decimal PercentageChange { get; set; }

        /// <summary>
        /// Previous investment amount before the change
        /// </summary>
        public decimal PreviousInvestment { get; set; }

        /// <summary>
        /// New investment amount after the change
        /// </summary>
        public decimal NewInvestment { get; set; }

        /// <summary>
        /// Change in investment amount
        /// </summary>
        public decimal InvestmentChange { get; set; }

        /// <summary>
        /// Type of change (Initial, Adjustment, Transfer, etc.)
        /// </summary>
        public EOwnershipChangeType? ChangeTypeEnum { get; set; }

        /// <summary>
        /// Reason for the ownership change
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// User who initiated/approved this change
        /// </summary>
        public int? ChangedByUserId { get; set; }

        /// <summary>
        /// Date when this change was recorded
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Navigation property to vehicle
        /// </summary>
        public virtual Vehicle Vehicle { get; set; } = null!;

        /// <summary>
        /// Navigation property to co-owner
        /// </summary>
        public virtual CoOwner CoOwner { get; set; } = null!;

        /// <summary>
        /// Navigation property to user
        /// </summary>
        public virtual User User { get; set; } = null!;

        /// <summary>
        /// Navigation property to ownership change request (if applicable)
        /// </summary>
        public virtual OwnershipChangeRequest? OwnershipChangeRequest { get; set; }

        /// <summary>
        /// Navigation property to user who initiated the change
        /// </summary>
        public virtual User? ChangedByUser { get; set; }
    }
}
