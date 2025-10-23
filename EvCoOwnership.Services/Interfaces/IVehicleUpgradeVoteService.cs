using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.UpgradeVoteDTOs;

namespace EvCoOwnership.Services.Interfaces
{
    /// <summary>
    /// Service interface for managing vehicle upgrade proposals and voting
    /// </summary>
    public interface IVehicleUpgradeVoteService
    {
        /// <summary>
        /// Propose a new vehicle upgrade
        /// </summary>
        /// <param name="request">The upgrade proposal details</param>
        /// <param name="userId">The ID of the user proposing the upgrade</param>
        /// <returns>Response with the created proposal</returns>
        Task<BaseResponse<VehicleUpgradeProposalResponse>> ProposeVehicleUpgradeAsync(ProposeVehicleUpgradeRequest request, Guid userId);

        /// <summary>
        /// Vote on a vehicle upgrade proposal
        /// </summary>
        /// <param name="proposalId">The ID of the proposal to vote on</param>
        /// <param name="request">The vote details (approve/reject + comments)</param>
        /// <param name="userId">The ID of the user casting the vote</param>
        /// <returns>Response with updated proposal status</returns>
        Task<BaseResponse<VehicleUpgradeProposalResponse>> VoteOnUpgradeAsync(Guid proposalId, VoteVehicleUpgradeRequest request, Guid userId);

        /// <summary>
        /// Get detailed information about a specific upgrade proposal
        /// </summary>
        /// <param name="proposalId">The ID of the proposal</param>
        /// <param name="userId">The ID of the requesting user</param>
        /// <returns>Response with proposal details including voting statistics</returns>
        Task<BaseResponse<VehicleUpgradeProposalResponse>> GetUpgradeProposalDetailsAsync(Guid proposalId, Guid userId);

        /// <summary>
        /// Get all pending upgrade proposals for a specific vehicle
        /// </summary>
        /// <param name="vehicleId">The ID of the vehicle</param>
        /// <param name="userId">The ID of the requesting user</param>
        /// <returns>Response with list of pending proposals</returns>
        Task<BaseResponse<PendingUpgradeProposalsSummary>> GetPendingUpgradesForVehicleAsync(Guid vehicleId, Guid userId);

        /// <summary>
        /// Mark an approved upgrade proposal as executed
        /// </summary>
        /// <param name="proposalId">The ID of the approved proposal</param>
        /// <param name="request">The execution details (actual cost, notes, invoice)</param>
        /// <param name="userId">The ID of the user marking as executed (admin or proposer)</param>
        /// <returns>Response with updated proposal status</returns>
        Task<BaseResponse<VehicleUpgradeProposalResponse>> MarkUpgradeAsExecutedAsync(Guid proposalId, MarkUpgradeExecutedRequest request, Guid userId);

        /// <summary>
        /// Cancel an upgrade proposal
        /// </summary>
        /// <param name="proposalId">The ID of the proposal to cancel</param>
        /// <param name="userId">The ID of the user cancelling (admin or proposer)</param>
        /// <returns>Response confirming cancellation</returns>
        Task<BaseResponse<VehicleUpgradeProposalResponse>> CancelUpgradeProposalAsync(Guid proposalId, Guid userId);

        /// <summary>
        /// Get the user's upgrade voting history
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <returns>Response with voting history</returns>
        Task<BaseResponse<List<UserUpgradeVotingHistoryResponse>>> GetUserUpgradeVotingHistoryAsync(Guid userId);

        /// <summary>
        /// Get upgrade statistics for a specific vehicle
        /// </summary>
        /// <param name="vehicleId">The ID of the vehicle</param>
        /// <param name="userId">The ID of the requesting user</param>
        /// <returns>Response with upgrade statistics</returns>
        Task<BaseResponse<VehicleUpgradeStatistics>> GetVehicleUpgradeStatisticsAsync(Guid vehicleId, Guid userId);
    }
}
