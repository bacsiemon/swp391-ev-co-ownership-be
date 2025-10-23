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

        /// <summary>
        /// Gets all available vehicles for co-ownership or booking
        /// </summary>
        /// <param name="pageIndex">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 10)</param>
        /// <param name="filterByStatus">Filter by vehicle status (optional)</param>
        /// <param name="filterByVerificationStatus">Filter by verification status (optional)</param>
        /// <returns>Response containing paginated list of available vehicles</returns>
        /// <summary>
        /// Gets available vehicles based on user role with comprehensive filters
        /// Co-owner: only their group's vehicles
        /// Staff/Admin: all vehicles
        /// </summary>
        Task<BaseResponse> GetAvailableVehiclesAsync(
            int userId,
            int pageIndex = 1,
            int pageSize = 10,
            string? filterByStatus = null,
            string? filterByVerificationStatus = null,
            string? brand = null,
            string? model = null,
            int? minYear = null,
            int? maxYear = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? search = null,
            string? sortBy = null,
            bool sortDescending = true);

        /// <summary>
        /// Gets detailed vehicle information including fund, co-owners, and creator info
        /// Role-based access: Co-owners can view their group's vehicles, Staff/Admin can view all
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="userId">ID of the requesting user</param>
        /// <returns>Response containing detailed vehicle information</returns>
        Task<BaseResponse> GetVehicleDetailAsync(int vehicleId, int userId);

        // Vehicle Availability Methods

        /// <summary>
        /// Gets vehicle availability schedule showing booked and free time slots
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="userId">ID of the requesting user</param>
        /// <param name="startDate">Start date of the period</param>
        /// <param name="endDate">End date of the period</param>
        /// <param name="statusFilter">Optional: Filter bookings by status</param>
        /// <returns>Response containing vehicle availability schedule and utilization stats</returns>
        Task<BaseResponse> GetVehicleAvailabilityScheduleAsync(
            int vehicleId, 
            int userId, 
            DateTime startDate, 
            DateTime endDate, 
            string? statusFilter = null);

        /// <summary>
        /// Finds available time slots for a vehicle within a date range
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="userId">ID of the requesting user</param>
        /// <param name="startDate">Start date to search</param>
        /// <param name="endDate">End date to search</param>
        /// <param name="minimumDurationHours">Minimum duration required (hours)</param>
        /// <param name="fullDayOnly">Only return full-day slots</param>
        /// <returns>Response containing available time slots</returns>
        Task<BaseResponse> FindAvailableTimeSlotsAsync(
            int vehicleId, 
            int userId, 
            DateTime startDate, 
            DateTime endDate, 
            int minimumDurationHours = 1, 
            bool fullDayOnly = false);

        /// <summary>
        /// Compares utilization of multiple vehicles in user's group
        /// Co-owner: vehicles in their groups
        /// Staff/Admin: all vehicles
        /// </summary>
        /// <param name="userId">ID of the requesting user</param>
        /// <param name="startDate">Start date of comparison period</param>
        /// <param name="endDate">End date of comparison period</param>
        /// <returns>Response containing vehicle utilization comparison</returns>
        Task<BaseResponse> CompareVehicleUtilizationAsync(
            int userId, 
            DateTime startDate, 
            DateTime endDate);
    }
}