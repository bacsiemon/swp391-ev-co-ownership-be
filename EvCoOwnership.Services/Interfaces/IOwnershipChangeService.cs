using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.OwnershipDTOs;

namespace EvCoOwnership.Services.Interfaces
{
    /// <summary>
    /// Service interface for managing ownership percentage changes with group consensus
    /// </summary>
    public interface IOwnershipChangeService
    {
        /// <summary>
        /// Proposes a change to vehicle ownership percentages
        /// Requires approval from all affected co-owners
        /// </summary>
        /// <param name="request">Ownership change proposal details</param>
        /// <param name="proposedByUserId">ID of user proposing the change</param>
        /// <returns>Response with created ownership change request details</returns>
        Task<BaseResponse<OwnershipChangeRequestResponse>> ProposeOwnershipChangeAsync(
            ProposeOwnershipChangeRequest request,
            int proposedByUserId);

        /// <summary>
        /// Gets details of a specific ownership change request
        /// </summary>
        /// <param name="requestId">Ownership change request ID</param>
        /// <param name="userId">ID of user making the request (for authorization)</param>
        /// <returns>Response with ownership change request details</returns>
        Task<BaseResponse<OwnershipChangeRequestResponse>> GetOwnershipChangeRequestAsync(
            int requestId,
            int userId);

        /// <summary>
        /// Gets all ownership change requests for a specific vehicle
        /// </summary>
        /// <param name="vehicleId">Vehicle ID</param>
        /// <param name="userId">ID of user making the request (for authorization)</param>
        /// <param name="includeCompleted">Include completed (approved/rejected) requests</param>
        /// <returns>Response with list of ownership change requests</returns>
        Task<BaseResponse<List<OwnershipChangeRequestResponse>>> GetVehicleOwnershipChangeRequestsAsync(
            int vehicleId,
            int userId,
            bool includeCompleted = false);

        /// <summary>
        /// Gets all pending ownership change requests requiring approval from the user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Response with list of pending ownership change requests</returns>
        Task<BaseResponse<List<OwnershipChangeRequestResponse>>> GetPendingApprovalsAsync(int userId);

        /// <summary>
        /// Approves or rejects an ownership change request
        /// When all co-owners approve, the change is automatically applied
        /// If any co-owner rejects, the request is marked as rejected
        /// </summary>
        /// <param name="requestId">Ownership change request ID</param>
        /// <param name="request">Approval decision and comments</param>
        /// <param name="userId">ID of user approving/rejecting</param>
        /// <returns>Response with updated ownership change request details</returns>
        Task<BaseResponse<OwnershipChangeRequestResponse>> ApproveOrRejectOwnershipChangeAsync(
            int requestId,
            ApproveOwnershipChangeRequest request,
            int userId);

        /// <summary>
        /// Cancels a pending ownership change request
        /// Only the proposer can cancel the request
        /// </summary>
        /// <param name="requestId">Ownership change request ID</param>
        /// <param name="userId">ID of user cancelling (must be proposer)</param>
        /// <returns>Response indicating cancellation success</returns>
        Task<BaseResponse<bool>> CancelOwnershipChangeRequestAsync(int requestId, int userId);

        /// <summary>
        /// Gets statistics about ownership change requests (Admin only)
        /// </summary>
        /// <returns>Response with ownership change statistics</returns>
        Task<BaseResponse<OwnershipChangeStatisticsResponse>> GetOwnershipChangeStatisticsAsync();

        /// <summary>
        /// Gets all ownership change requests for a user (as proposer or approver)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="includeCompleted">Include completed requests</param>
        /// <returns>Response with list of ownership change requests</returns>
        Task<BaseResponse<List<OwnershipChangeRequestResponse>>> GetUserOwnershipChangeRequestsAsync(
            int userId,
            bool includeCompleted = false);

        // Ownership History Methods

        /// <summary>
        /// Gets ownership history for a specific vehicle
        /// </summary>
        /// <param name="vehicleId">Vehicle ID</param>
        /// <param name="userId">ID of user making the request (for authorization)</param>
        /// <param name="request">Filter parameters (optional)</param>
        /// <returns>Response with list of ownership history records</returns>
        Task<BaseResponse<List<OwnershipHistoryResponse>>> GetVehicleOwnershipHistoryAsync(
            int vehicleId,
            int userId,
            GetOwnershipHistoryRequest? request = null);

        /// <summary>
        /// Gets ownership timeline showing evolution of ownership for all co-owners
        /// </summary>
        /// <param name="vehicleId">Vehicle ID</param>
        /// <param name="userId">ID of user making the request (for authorization)</param>
        /// <returns>Response with complete ownership timeline</returns>
        Task<BaseResponse<VehicleOwnershipTimelineResponse>> GetVehicleOwnershipTimelineAsync(
            int vehicleId,
            int userId);

        /// <summary>
        /// Gets ownership snapshot at a specific point in time
        /// </summary>
        /// <param name="vehicleId">Vehicle ID</param>
        /// <param name="snapshotDate">Date for the snapshot</param>
        /// <param name="userId">ID of user making the request (for authorization)</param>
        /// <returns>Response with ownership snapshot</returns>
        Task<BaseResponse<OwnershipSnapshotResponse>> GetOwnershipSnapshotAsync(
            int vehicleId,
            DateTime snapshotDate,
            int userId);

        /// <summary>
        /// Gets ownership history statistics for a vehicle
        /// </summary>
        /// <param name="vehicleId">Vehicle ID</param>
        /// <param name="userId">ID of user making the request (for authorization)</param>
        /// <returns>Response with ownership history statistics</returns>
        Task<BaseResponse<OwnershipHistoryStatisticsResponse>> GetOwnershipHistoryStatisticsAsync(
            int vehicleId,
            int userId);

        /// <summary>
        /// Gets ownership history for a specific co-owner across all vehicles
        /// </summary>
        /// <param name="userId">User ID of the co-owner</param>
        /// <returns>Response with ownership history for the co-owner</returns>
        Task<BaseResponse<List<OwnershipHistoryResponse>>> GetCoOwnerOwnershipHistoryAsync(int userId);
    }
}
