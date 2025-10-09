using EvCoOwnership.Helpers.BaseClasses;

namespace EvCoOwnership.Services.Interfaces
{
    /// <summary>
    /// Interface for managing Co-owner eligibility in the EV Co-ownership system
    /// </summary>
    public interface ICoOwnerEligibilityService
    {
        /// <summary>
        /// Checks if a user meets all requirements to become a Co-owner
        /// </summary>
        /// <param name="userId">User ID to check</param>
        /// <returns>BaseResponse with eligibility status and requirements</returns>
        Task<BaseResponse> CheckCoOwnerEligibilityAsync(int userId);

        /// <summary>
        /// Promotes an eligible user to Co-owner status
        /// </summary>
        /// <param name="userId">User ID to promote</param>
        /// <returns>BaseResponse with promotion result</returns>
        Task<BaseResponse> PromoteToCoOwnerAsync(int userId);

        /// <summary>
        /// Gets system-wide Co-ownership statistics
        /// </summary>
        /// <returns>BaseResponse with statistical data</returns>
        Task<BaseResponse> GetCoOwnershipStatsAsync();
    }
}