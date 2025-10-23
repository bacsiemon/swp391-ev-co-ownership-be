using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.DisputeDTOs;

namespace EvCoOwnership.Services.Interfaces
{
    /// <summary>
    /// Service interface for dispute management
    /// </summary>
    public interface IDisputeService
    {
        /// <summary>
        /// Raise a booking dispute
        /// </summary>
        /// <param name="userId">User raising the dispute</param>
        /// <param name="request">Dispute details</param>
        /// <returns>Created dispute</returns>
        Task<BaseResponse<DisputeResponse>> RaiseBookingDisputeAsync(
            int userId,
            RaiseBookingDisputeRequest request);

        /// <summary>
        /// Raise a cost sharing dispute
        /// </summary>
        /// <param name="userId">User raising the dispute</param>
        /// <param name="request">Dispute details</param>
        /// <returns>Created dispute</returns>
        Task<BaseResponse<DisputeResponse>> RaiseCostSharingDisputeAsync(
            int userId,
            RaiseCostSharingDisputeRequest request);

        /// <summary>
        /// Raise a group decision dispute
        /// </summary>
        /// <param name="userId">User raising the dispute</param>
        /// <param name="request">Dispute details</param>
        /// <returns>Created dispute</returns>
        Task<BaseResponse<DisputeResponse>> RaiseGroupDecisionDisputeAsync(
            int userId,
            RaiseGroupDecisionDisputeRequest request);

        /// <summary>
        /// Get dispute by ID
        /// </summary>
        /// <param name="disputeId">Dispute ID</param>
        /// <param name="userId">Requesting user ID</param>
        /// <returns>Dispute details</returns>
        Task<BaseResponse<DisputeResponse>> GetDisputeByIdAsync(
            int disputeId,
            int userId);

        /// <summary>
        /// Get list of disputes with filters
        /// </summary>
        /// <param name="userId">Requesting user ID</param>
        /// <param name="request">Filter criteria</param>
        /// <returns>List of disputes</returns>
        Task<BaseResponse<DisputeListResponse>> GetDisputesAsync(
            int userId,
            GetDisputesRequest request);

        /// <summary>
        /// Respond to a dispute
        /// </summary>
        /// <param name="disputeId">Dispute ID</param>
        /// <param name="userId">Responding user ID</param>
        /// <param name="request">Response details</param>
        /// <returns>Updated dispute</returns>
        Task<BaseResponse<DisputeResponse>> RespondToDisputeAsync(
            int disputeId,
            int userId,
            RespondToDisputeRequest request);

        /// <summary>
        /// Update dispute status (Admin/Mediator only)
        /// </summary>
        /// <param name="disputeId">Dispute ID</param>
        /// <param name="userId">Admin/mediator user ID</param>
        /// <param name="request">Status update details</param>
        /// <returns>Updated dispute</returns>
        Task<BaseResponse<DisputeResponse>> UpdateDisputeStatusAsync(
            int disputeId,
            int userId,
            UpdateDisputeStatusRequest request);

        /// <summary>
        /// Withdraw a dispute (Initiator only)
        /// </summary>
        /// <param name="disputeId">Dispute ID</param>
        /// <param name="userId">Initiator user ID</param>
        /// <param name="reason">Reason for withdrawal</param>
        /// <returns>Updated dispute</returns>
        Task<BaseResponse<DisputeResponse>> WithdrawDisputeAsync(
            int disputeId,
            int userId,
            string reason);
    }
}
