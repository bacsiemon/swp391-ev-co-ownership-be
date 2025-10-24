using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Repositories.DTOs.UpgradeVoteDTOs
{
    /// <summary>
    /// Types of vehicle upgrades that can be proposed
    /// </summary>
    public enum EUpgradeType
    {
        /// <summary>
        /// Battery upgrade or replacement
        /// </summary>
        BatteryUpgrade = 0,

        /// <summary>
        /// Insurance package upgrade
        /// </summary>
        InsurancePackage = 1,

        /// <summary>
        /// Technology and software upgrades
        /// </summary>
        TechnologyUpgrade = 2,

        /// <summary>
        /// Interior or comfort upgrades
        /// </summary>
        InteriorUpgrade = 3,

        /// <summary>
        /// Performance and mechanical upgrades
        /// </summary>
        PerformanceUpgrade = 4,

        /// <summary>
        /// Safety features and equipment
        /// </summary>
        SafetyUpgrade = 5,

        /// <summary>
        /// Other miscellaneous upgrades
        /// </summary>
        Other = 6
    }

    /// <summary>
    /// Request to propose a vehicle upgrade
    /// </summary>
    public class ProposeVehicleUpgradeRequest
    {
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
    }

    /// <summary>
    /// Request to vote on a vehicle upgrade proposal
    /// </summary>
    public class VoteVehicleUpgradeRequest
    {
        public bool Approve { get; set; }
        public string? Comments { get; set; }
    }

    /// <summary>
    /// Response for a vehicle upgrade proposal
    /// </summary>
    public class VehicleUpgradeProposalResponse
    {
        public int ProposalId { get; set; }
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public EUpgradeType UpgradeType { get; set; }
        public string UpgradeTypeName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal EstimatedCost { get; set; }
        public string? Justification { get; set; }
        public string? ImageUrl { get; set; }
        public string? VendorName { get; set; }
        public string? VendorContact { get; set; }
        public DateTime? ProposedInstallationDate { get; set; }
        public int? EstimatedDurationDays { get; set; }
        
        // Proposer information
        public int ProposedByUserId { get; set; }
        public string ProposedByUserName { get; set; } = string.Empty;
        public DateTime ProposedAt { get; set; }
        
        // Voting statistics
        public int TotalCoOwners { get; set; }
        public int RequiredApprovals { get; set; }
        public int CurrentApprovals { get; set; }
        public int CurrentRejections { get; set; }
        public decimal ApprovalPercentage { get; set; }
        public string VotingStatus { get; set; } = string.Empty; // "Pending", "Approved", "Rejected", "Cancelled"
        public bool IsApproved { get; set; }
        public bool IsRejected { get; set; }
        public bool IsCancelled { get; set; }
        
        // Execution details
        public bool IsExecuted { get; set; }
        public DateTime? ExecutedAt { get; set; }
        public decimal? ActualCost { get; set; }
        public string? ExecutionNotes { get; set; }
        
        // Vote details
        public List<UpgradeVoteDetailResponse> Votes { get; set; } = new();
    }

    /// <summary>
    /// Individual vote detail for upgrade proposal
    /// </summary>
    public class UpgradeVoteDetailResponse
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public bool? HasVoted { get; set; }
        public bool? IsAgree { get; set; }
        public string? Comments { get; set; }
        public DateTime? VotedAt { get; set; }
    }

    /// <summary>
    /// Summary of pending upgrade proposals for a vehicle
    /// </summary>
    public class PendingUpgradeProposalsSummary
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public int TotalPendingProposals { get; set; }
        public decimal TotalPendingCost { get; set; }
        public List<VehicleUpgradeProposalResponse> Proposals { get; set; } = new();
    }

    /// <summary>
    /// Request to mark proposal as executed
    /// </summary>
    public class MarkUpgradeExecutedRequest
    {
        public decimal ActualCost { get; set; }
        public string? ExecutionNotes { get; set; }
        public string? InvoiceImageUrl { get; set; }
    }

    /// <summary>
    /// Upgrade voting history for a user
    /// </summary>
    public class UserUpgradeVotingHistoryResponse
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int TotalProposalsCreated { get; set; }
        public int TotalVotesCast { get; set; }
        public int ApprovalsGiven { get; set; }
        public int RejectionsGiven { get; set; }
        public int PendingVotes { get; set; }
        public List<VehicleUpgradeProposalResponse> ProposalHistory { get; set; } = new();
    }

    /// <summary>
    /// Upgrade statistics for a vehicle
    /// </summary>
    public class VehicleUpgradeStatistics
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public int TotalUpgradesCompleted { get; set; }
        public decimal TotalUpgradeCost { get; set; }
        public int PendingProposals { get; set; }
        public int RejectedProposals { get; set; }
        public Dictionary<string, int> UpgradesByType { get; set; } = new();
        public List<VehicleUpgradeProposalResponse> RecentUpgrades { get; set; } = new();
    }
}
