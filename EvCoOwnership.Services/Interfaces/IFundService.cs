using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.FundDTOs;

namespace EvCoOwnership.Services.Interfaces
{
    /// <summary>
    /// Service interface for fund management operations
    /// </summary>
    public interface IFundService
    {
        /// <summary>
        /// Gets current fund balance for a vehicle
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="requestingUserId">ID of the user making the request</param>
        /// <returns>Fund balance information</returns>
        Task<BaseResponse<FundBalanceResponse>> GetFundBalanceAsync(int vehicleId, int requestingUserId);

        /// <summary>
        /// Gets fund additions history for a vehicle
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="requestingUserId">ID of the user making the request</param>
        /// <param name="pageNumber">Page number for pagination</param>
        /// <param name="pageSize">Page size for pagination</param>
        /// <returns>List of fund additions</returns>
        Task<BaseResponse<List<FundAdditionResponse>>> GetFundAdditionsAsync(
            int vehicleId, 
            int requestingUserId, 
            int pageNumber = 1, 
            int pageSize = 20);

        /// <summary>
        /// Gets fund usages history for a vehicle
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="requestingUserId">ID of the user making the request</param>
        /// <param name="pageNumber">Page number for pagination</param>
        /// <param name="pageSize">Page size for pagination</param>
        /// <returns>List of fund usages</returns>
        Task<BaseResponse<List<FundUsageResponse>>> GetFundUsagesAsync(
            int vehicleId, 
            int requestingUserId, 
            int pageNumber = 1, 
            int pageSize = 20);

        /// <summary>
        /// Gets comprehensive fund summary with statistics
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="requestingUserId">ID of the user making the request</param>
        /// <param name="monthsToAnalyze">Number of months to analyze for statistics (default 6)</param>
        /// <returns>Complete fund summary with balance, history, and statistics</returns>
        Task<BaseResponse<FundSummaryResponse>> GetFundSummaryAsync(
            int vehicleId, 
            int requestingUserId, 
            int monthsToAnalyze = 6);
    }
}
