using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.UsageAnalyticsDTOs;

namespace EvCoOwnership.Services.Interfaces
{
    /// <summary>
    /// Service interface for usage analytics and comparisons
    /// </summary>
    public interface IUsageAnalyticsService
    {
        /// <summary>
        /// Get usage vs ownership comparison data for a vehicle
        /// </summary>
        /// <param name="vehicleId">Vehicle ID</param>
        /// <param name="userId">Requesting user ID (for authorization)</param>
        /// <param name="request">Filter and metric options</param>
        /// <returns>Usage vs ownership comparison data</returns>
        Task<BaseResponse<UsageVsOwnershipResponse>> GetUsageVsOwnershipAsync(
            int vehicleId,
            int userId,
            GetUsageVsOwnershipRequest? request = null);

        /// <summary>
        /// Get time-series trends of usage vs ownership over time
        /// </summary>
        /// <param name="vehicleId">Vehicle ID</param>
        /// <param name="userId">Requesting user ID (for authorization)</param>
        /// <param name="startDate">Analysis start date</param>
        /// <param name="endDate">Analysis end date</param>
        /// <param name="granularity">Time granularity: Daily, Weekly, Monthly</param>
        /// <returns>Time-series trend data</returns>
        Task<BaseResponse<UsageVsOwnershipTrendsResponse>> GetUsageVsOwnershipTrendsAsync(
            int vehicleId,
            int userId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string granularity = "Monthly");

        /// <summary>
        /// Get detailed usage breakdown for a specific co-owner
        /// </summary>
        /// <param name="vehicleId">Vehicle ID</param>
        /// <param name="coOwnerId">Co-owner ID</param>
        /// <param name="userId">Requesting user ID (for authorization)</param>
        /// <param name="startDate">Analysis start date</param>
        /// <param name="endDate">Analysis end date</param>
        /// <returns>Detailed co-owner usage breakdown</returns>
        Task<BaseResponse<CoOwnerUsageDetailResponse>> GetCoOwnerUsageDetailAsync(
            int vehicleId,
            int coOwnerId,
            int userId,
            DateTime? startDate = null,
            DateTime? endDate = null);
    }
}
