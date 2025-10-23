using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Repositories.DTOs.MaintenanceVoteDTOs
{
    /// <summary>
    /// Request to propose a maintenance expenditure that requires co-owner voting
    /// </summary>
    public class ProposeMaintenanceExpenditureRequest
    {
        public int VehicleId { get; set; }
        public int MaintenanceCostId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? ImageUrl { get; set; }
    }

    /// <summary>
    /// Request to vote on a proposed maintenance expenditure
    /// </summary>
    public class VoteMaintenanceExpenditureRequest
    {
        public bool Approve { get; set; }
        public string? Comments { get; set; }
    }

    /// <summary>
    /// Response for a maintenance expenditure proposal
    /// </summary>
    public class MaintenanceExpenditureProposalResponse
    {
        public int FundUsageId { get; set; }
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public int MaintenanceCostId { get; set; }
        public string MaintenanceDescription { get; set; } = string.Empty;
        public EMaintenanceType MaintenanceType { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int ProposedByUserId { get; set; }
        public string ProposedByUserName { get; set; } = string.Empty;
        public DateTime ProposedAt { get; set; }
        
        // Voting statistics
        public int TotalCoOwners { get; set; }
        public int RequiredApprovals { get; set; }
        public int CurrentApprovals { get; set; }
        public int CurrentRejections { get; set; }
        public decimal ApprovalPercentage { get; set; }
        public string VotingStatus { get; set; } = string.Empty; // "Pending", "Approved", "Rejected"
        public bool IsApproved { get; set; }
        public bool IsRejected { get; set; }
        
        // Vote details
        public List<VoteDetailResponse> Votes { get; set; } = new();
    }

    /// <summary>
    /// Individual vote detail
    /// </summary>
    public class VoteDetailResponse
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public bool? HasVoted { get; set; }
        public bool? IsAgree { get; set; }
        public DateTime? VotedAt { get; set; }
    }

    /// <summary>
    /// Summary of all pending maintenance expenditure proposals for a vehicle
    /// </summary>
    public class PendingMaintenanceProposalsSummary
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public int TotalPendingProposals { get; set; }
        public decimal TotalPendingAmount { get; set; }
        public List<MaintenanceExpenditureProposalResponse> Proposals { get; set; } = new();
    }

    /// <summary>
    /// Voting history for a user
    /// </summary>
    public class UserVotingHistoryResponse
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int TotalVotesCast { get; set; }
        public int ApprovalsGiven { get; set; }
        public int RejectionsGiven { get; set; }
        public int PendingVotes { get; set; }
        public List<MaintenanceExpenditureProposalResponse> VotingHistory { get; set; } = new();
    }
}
