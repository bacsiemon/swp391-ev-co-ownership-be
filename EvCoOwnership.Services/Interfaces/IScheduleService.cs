using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.ScheduleDTOs;

namespace EvCoOwnership.Services.Interfaces
{
    /// <summary>
    /// Service for managing vehicle schedules and availability
    /// </summary>
    public interface IScheduleService
    {
        /// <summary>
        /// Get vehicle schedule for a specific period
        /// </summary>
        /// <param name="request">Schedule request parameters</param>
        /// <param name="userId">User ID making the request</param>
        /// <returns>Vehicle schedule response</returns>
        Task<BaseResponse<VehicleScheduleResponse>> GetVehicleScheduleAsync(GetVehicleScheduleRequest request, int userId);

        /// <summary>
        /// Check availability for a specific time slot
        /// </summary>
        /// <param name="request">Availability check request</param>
        /// <param name="userId">User ID making the request</param>
        /// <returns>Availability check response</returns>
        Task<BaseResponse<AvailabilityCheckResponse>> CheckAvailabilityAsync(CheckAvailabilityRequest request, int userId);

        /// <summary>
        /// Find optimal booking slots based on preferences
        /// </summary>
        /// <param name="request">Optimal slots search request</param>
        /// <param name="userId">User ID making the request</param>
        /// <returns>Optimal slots response</returns>
        Task<BaseResponse<OptimalSlotsResponse>> FindOptimalSlotsAsync(FindOptimalSlotsRequest request, int userId);

        /// <summary>
        /// Get user's personal booking schedule
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="startDate">Start date for schedule</param>
        /// <param name="endDate">End date for schedule</param>
        /// <returns>User's booking schedule</returns>
        Task<BaseResponse<List<ScheduleBookingSlot>>> GetUserScheduleAsync(int userId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Get schedule conflicts for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="startDate">Start date to check</param>
        /// <param name="endDate">End date to check</param>
        /// <returns>List of schedule conflicts</returns>
        Task<BaseResponse<List<ConflictingBooking>>> GetScheduleConflictsAsync(int userId, DateTime startDate, DateTime endDate);
    }
}