using EvCoOwnership.DTOs.VehicleDTOs;
using EvCoOwnership.Helpers.BaseClasses;

namespace EvCoOwnership.Services.Interfaces
{
    /// <summary>
    /// Service interface for vehicle management operations
    /// </summary>
    public interface IVehicleService
    {
        /// <summary>
        /// Creates a new vehicle and assigns the creator as the primary owner
        /// </summary>
        /// <param name="request">Vehicle creation request</param>
        /// <param name="createdById">ID of the user creating the vehicle</param>
        /// <returns>Response containing the created vehicle information</returns>
        Task<BaseResponse> CreateVehicleAsync(CreateVehicleRequest request, int createdById);

        /// <summary>
        /// Adds a co-owner to an existing vehicle by sending an invitation
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="request">Add co-owner request</param>
        /// <param name="invitedById">ID of the user sending the invitation</param>
        /// <returns>Response containing the invitation result</returns>
        Task<BaseResponse> AddCoOwnerAsync(int vehicleId, AddCoOwnerRequest request, int invitedById);

        /// <summary>
        /// Responds to a co-ownership invitation (accept or reject)
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="request">Response to invitation request</param>
        /// <param name="userId">ID of the user responding to the invitation</param>
        /// <returns>Response containing the result of the invitation response</returns>
        Task<BaseResponse> RespondToInvitationAsync(int vehicleId, RespondToInvitationRequest request, int userId);

        /// <summary>
        /// Gets vehicle information including co-owners
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="userId">ID of the requesting user</param>
        /// <returns>Response containing vehicle information</returns>
        Task<BaseResponse> GetVehicleAsync(int vehicleId, int userId);

        /// <summary>
        /// Gets all vehicles for a specific user (as owner or co-owner)
        /// </summary>
        /// <param name="userId">ID of the user</param>
        /// <returns>Response containing list of user's vehicles</returns>
        Task<BaseResponse> GetUserVehiclesAsync(int userId);

        /// <summary>
        /// Gets pending co-ownership invitations for a user
        /// </summary>
        /// <param name="userId">ID of the user</param>
        /// <returns>Response containing pending invitations</returns>
        Task<BaseResponse> GetPendingInvitationsAsync(int userId);

        /// <summary>
        /// Validates if a user can create a vehicle (has valid license, etc.)
        /// </summary>
        /// <param name="userId">ID of the user</param>
        /// <returns>Response indicating if user can create vehicle</returns>
        Task<BaseResponse> ValidateVehicleCreationEligibilityAsync(int userId);

        /// <summary>
        /// Validates if a vehicle's ownership percentages and invitations are valid
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="newOwnershipPercentage">New ownership percentage to add</param>
        /// <returns>Response indicating if the ownership distribution is valid</returns>
        Task<BaseResponse> ValidateOwnershipPercentageAsync(int vehicleId, decimal newOwnershipPercentage);

        /// <summary>
        /// Removes a co-owner from a vehicle (only by vehicle creator or admin)
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="coOwnerUserId">ID of the co-owner to remove</param>
        /// <param name="requestingUserId">ID of the user making the request</param>
        /// <returns>Response containing the removal result</returns>
        Task<BaseResponse> RemoveCoOwnerAsync(int vehicleId, int coOwnerUserId, int requestingUserId);

        /// <summary>
        /// Updates vehicle information (only by owners)
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="request">Vehicle update request</param>
        /// <param name="userId">ID of the user making the request</param>
        /// <returns>Response containing the update result</returns>
        Task<BaseResponse> UpdateVehicleAsync(int vehicleId, CreateVehicleRequest request, int userId);
    }
}