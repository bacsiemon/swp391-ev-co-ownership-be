using EvCoOwnership.Repositories.DTOs.UserDTOs;
using EvCoOwnership.Helpers.BaseClasses;
using Microsoft.AspNetCore.Http;

namespace EvCoOwnership.Services.Interfaces
{
    /// <summary>
    /// Interface for User Profile Service
    /// </summary>
    public interface IUserProfileService
    {
        /// <summary>
        /// Gets user profile with statistics
        /// </summary>
        Task<BaseResponse<UserProfileResponse>> GetUserProfileAsync(int userId);

        /// <summary>
        /// Updates user profile information
        /// </summary>
        Task<BaseResponse<UserProfileResponse>> UpdateUserProfileAsync(int userId, UpdateUserProfileRequest request);

        /// <summary>
        /// Changes user password
        /// </summary>
        Task<BaseResponse<object>> ChangePasswordAsync(int userId, ChangePasswordRequest request);

        /// <summary>
        /// Gets user vehicles summary
        /// </summary>
        Task<BaseResponse<UserVehiclesSummary>> GetUserVehiclesSummaryAsync(int userId);

        /// <summary>
        /// Gets user activity summary
        /// </summary>
        Task<BaseResponse<UserActivitySummary>> GetUserActivitySummaryAsync(int userId);

        /// <summary>
        /// Uploads profile image
        /// </summary>
        Task<BaseResponse<object>> UploadProfileImageAsync(int userId, IFormFile file);

        /// <summary>
        /// Deletes profile image
        /// </summary>
        Task<BaseResponse<object>> DeleteProfileImageAsync(int userId);

        /// <summary>
        /// Gets user profile by email
        /// </summary>
        Task<BaseResponse<UserProfileResponse>> GetUserProfileByEmailAsync(string email, int requestingUserId);

        /// <summary>
        /// Validates profile completeness
        /// </summary>
        Task<BaseResponse<UserProfileCompleteness>> ValidateProfileCompletenessAsync(int userId);
    }
}