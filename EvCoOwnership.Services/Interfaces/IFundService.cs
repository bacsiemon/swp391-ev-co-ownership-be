using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.FundDTOs;
using EvCoOwnership.Repositories.Enums;

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

        /// <summary>
        /// Creates a new fund usage record (expense)
        /// </summary>
        /// <param name="request">Fund usage creation request</param>
        /// <param name="requestingUserId">ID of the user making the request</param>
        /// <returns>Created fund usage record</returns>
        Task<BaseResponse<FundUsageResponse>> CreateFundUsageAsync(
            CreateFundUsageRequest request, 
            int requestingUserId);

        /// <summary>
        /// Updates an existing fund usage record
        /// </summary>
        /// <param name="usageId">ID of the fund usage to update</param>
        /// <param name="request">Update request</param>
        /// <param name="requestingUserId">ID of the user making the request</param>
        /// <returns>Updated fund usage record</returns>
        Task<BaseResponse<FundUsageResponse>> UpdateFundUsageAsync(
            int usageId, 
            UpdateFundUsageRequest request, 
            int requestingUserId);

        /// <summary>
        /// Deletes a fund usage record
        /// </summary>
        /// <param name="usageId">ID of the fund usage to delete</param>
        /// <param name="requestingUserId">ID of the user making the request</param>
        /// <returns>Success response</returns>
        Task<BaseResponse<object>> DeleteFundUsageAsync(
            int usageId, 
            int requestingUserId);

        /// <summary>
        /// Gets fund usages by category type
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="category">Usage category type</param>
        /// <param name="requestingUserId">ID of the user making the request</param>
        /// <param name="startDate">Optional start date filter</param>
        /// <param name="endDate">Optional end date filter</param>
        /// <returns>List of fund usages for the category</returns>
        Task<BaseResponse<List<FundUsageResponse>>> GetFundUsagesByCategoryAsync(
            int vehicleId, 
            EUsageType category, 
            int requestingUserId, 
            DateTime? startDate = null, 
            DateTime? endDate = null);

        /// <summary>
        /// Gets category-based budget analysis for current month
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="requestingUserId">ID of the user making the request</param>
        /// <returns>Category budget analysis</returns>
        Task<BaseResponse<FundCategoryAnalysisResponse>> GetCategoryBudgetAnalysisAsync(
            int vehicleId, 
            int requestingUserId);
    }
}
