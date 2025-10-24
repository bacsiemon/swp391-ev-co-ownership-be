using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.MaintenanceVoteDTOs;

namespace EvCoOwnership.Services.Interfaces
{
    /// <summary>
    /// Service interface for maintenance expenditure voting operations
    /// </summary>
    public interface IMaintenanceVoteService
    {
        /// <summary>
        /// Proposes a maintenance expenditure that requires co-owner voting approval
        /// </summary>
        Task<BaseResponse<MaintenanceExpenditureProposalResponse>> ProposeMaintenanceExpenditureAsync(
            ProposeMaintenanceExpenditureRequest request, 
            int proposerUserId);

        /// <summary>
        /// Votes (approve/reject) on a proposed maintenance expenditure
        /// </summary>
        Task<BaseResponse<MaintenanceExpenditureProposalResponse>> VoteOnMaintenanceExpenditureAsync(
            int fundUsageId, 
            VoteMaintenanceExpenditureRequest request, 
            int voterUserId);

        /// <summary>
        /// Gets details of a specific maintenance expenditure proposal
        /// </summary>
        Task<BaseResponse<MaintenanceExpenditureProposalResponse>> GetMaintenanceProposalDetailsAsync(
            int fundUsageId, 
            int requestingUserId);

        /// <summary>
        /// Gets all pending maintenance expenditure proposals for a vehicle
        /// </summary>
        Task<BaseResponse<PendingMaintenanceProposalsSummary>> GetPendingProposalsForVehicleAsync(
            int vehicleId, 
            int requestingUserId);

        /// <summary>
        /// Gets voting history for the requesting user
        /// </summary>
        Task<BaseResponse<UserVotingHistoryResponse>> GetUserVotingHistoryAsync(
            int userId);

        /// <summary>
        /// Cancels a pending maintenance expenditure proposal (only by proposer or admin)
        /// </summary>
        Task<BaseResponse<object>> CancelMaintenanceProposalAsync(
            int fundUsageId, 
            int requestingUserId);
    }
}
