using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.FairnessOptimizationDTOs;

namespace EvCoOwnership.Services.Interfaces
{
    /// <summary>
    /// Service interface for AI-powered fairness analysis and optimization recommendations
    /// </summary>
    public interface IFairnessOptimizationService
    {
        /// <summary>
        /// Generates comprehensive fairness report analyzing usage vs ownership patterns
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="userId">ID of the requesting user (must be co-owner)</param>
        /// <param name="request">Report generation parameters</param>
        /// <returns>Detailed fairness report with recommendations</returns>
        Task<BaseResponse<FairnessReportResponse>> GetFairnessReportAsync(
            int vehicleId,
            int userId,
            GetFairnessReportRequest request);

        /// <summary>
        /// Suggests optimal booking schedule to achieve fair usage distribution
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="userId">ID of the requesting user (must be co-owner)</param>
        /// <param name="request">Schedule suggestion parameters</param>
        /// <returns>Recommended booking slots for each co-owner</returns>
        Task<BaseResponse<FairScheduleSuggestionsResponse>> GetFairScheduleSuggestionsAsync(
            int vehicleId,
            int userId,
            GetFairScheduleSuggestionsRequest request);

        /// <summary>
        /// Provides predictive maintenance suggestions based on usage patterns and vehicle health
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="userId">ID of the requesting user (must be co-owner)</param>
        /// <param name="request">Maintenance suggestion parameters</param>
        /// <returns>Maintenance recommendations with cost forecasts</returns>
        Task<BaseResponse<MaintenanceSuggestionsResponse>> GetMaintenanceSuggestionsAsync(
            int vehicleId,
            int userId,
            GetMaintenanceSuggestionsRequest request);

        /// <summary>
        /// Analyzes costs and provides actionable recommendations for savings
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="userId">ID of the requesting user (must be co-owner)</param>
        /// <param name="request">Cost analysis parameters</param>
        /// <returns>Cost-saving recommendations with estimated savings</returns>
        Task<BaseResponse<CostSavingRecommendationsResponse>> GetCostSavingRecommendationsAsync(
            int vehicleId,
            int userId,
            GetCostSavingRecommendationsRequest request);
    }
}
