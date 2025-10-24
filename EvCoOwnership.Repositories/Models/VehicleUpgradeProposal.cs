using EvCoOwnership.Repositories.DTOs.UpgradeVoteDTOs;

namespace EvCoOwnership.Repositories.Models
{
    /// <summary>
    /// Represents a vehicle upgrade proposal that requires co-owner voting
    /// </summary>
    public partial class VehicleUpgradeProposal
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public EUpgradeType UpgradeType { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal EstimatedCost { get; set; }
        public string? Justification { get; set; }
        public string? ImageUrl { get; set; }
        public string? VendorName { get; set; }
        public string? VendorContact { get; set; }
        public DateTime? ProposedInstallationDate { get; set; }
        public int? EstimatedDurationDays { get; set; }
        
        public int ProposedByUserId { get; set; }
        public DateTime ProposedAt { get; set; }
        
        // Voting status
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Cancelled
        public DateTime? ApprovedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        
        // Execution tracking
        public bool IsExecuted { get; set; }
        public DateTime? ExecutedAt { get; set; }
        public decimal? ActualCost { get; set; }
        public string? ExecutionNotes { get; set; }
        public int? FundUsageId { get; set; } // Link to fund deduction after execution
        
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual Vehicle? Vehicle { get; set; }
        public virtual User? ProposedByUser { get; set; }
        public virtual FundUsage? FundUsage { get; set; }
        public virtual ICollection<VehicleUpgradeVote> Votes { get; set; } = new List<VehicleUpgradeVote>();
    }

    /// <summary>
    /// Represents a vote on a vehicle upgrade proposal
    /// </summary>
    public partial class VehicleUpgradeVote
    {
        public int ProposalId { get; set; }
        public int UserId { get; set; }
        public bool IsAgree { get; set; }
        public string? Comments { get; set; }
        public DateTime VotedAt { get; set; }

        // Navigation properties
        public virtual VehicleUpgradeProposal? Proposal { get; set; }
        public virtual User? User { get; set; }
    }
}
