using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.DTOs.UserDTOs;
using EvCoOwnership.Repositories.DTOs.ProfileDTOs;

namespace EvCoOwnership.Services.Interfaces
{
    public interface IProfileService
    {
        /// <summary>
        /// Get user profile information
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="requesterId">ID of user making the request (for privacy checks)</param>
        /// <returns>User profile data</returns>
        Task<BaseResponse<ProfileResponse>> GetProfileAsync(int userId, int requesterId);

        /// <summary>
        /// Update user profile information
        /// </summary>
        /// <param name="userId">User ID to update</param>
        /// <param name="request">Profile update data</param>
        /// <returns>Updated profile data</returns>
        Task<BaseResponse<ProfileResponse>> UpdateProfileAsync(int userId, UpdateProfileRequest request);

        /// <summary>
        /// Change user password
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="request">Password change request</param>
        /// <returns>Success status</returns>
        Task<BaseResponse<object>> ChangePasswordAsync(int userId, ChangePasswordRequest request);

        /// <summary>
        /// Update notification settings
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="request">Notification settings</param>
        /// <returns>Success status</returns>
        Task<BaseResponse<NotificationSettings>> UpdateNotificationSettingsAsync(int userId, UpdateNotificationSettingsRequest request);

        /// <summary>
        /// Update privacy settings
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="request">Privacy settings</param>
        /// <returns>Success status</returns>
        Task<BaseResponse<PrivacySettings>> UpdatePrivacySettingsAsync(int userId, UpdatePrivacySettingsRequest request);

        /// <summary>
        /// Get user activity log
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Items per page</param>
        /// <param name="category">Optional activity category filter</param>
        /// <returns>Activity log data</returns>
        Task<BaseResponse<ActivityLogResponse>> GetActivityLogAsync(int userId, int page = 1, int pageSize = 50, string? category = null);

        /// <summary>
        /// Get user security log
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="days">Number of days to look back (default 30)</param>
        /// <returns>Security log data</returns>
        Task<BaseResponse<SecurityLogResponse>> GetSecurityLogAsync(int userId, int days = 30);

        /// <summary>
        /// Delete user account (soft delete)
        /// </summary>
        /// <param name="userId">User ID to delete</param>
        /// <returns>Success status</returns>
        Task<BaseResponse<object>> DeleteAccountAsync(int userId);

        /// <summary>
        /// Upload profile avatar
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="avatarUrl">New avatar URL</param>
        /// <returns>Updated profile with new avatar</returns>
        Task<BaseResponse<ProfileResponse>> UpdateAvatarAsync(int userId, string avatarUrl);

        /// <summary>
        /// Verify email address
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="verificationToken">Email verification token</param>
        /// <returns>Success status</returns>
        Task<BaseResponse<object>> VerifyEmailAsync(int userId, string verificationToken);

        /// <summary>
        /// Send email verification
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Success status</returns>
        Task<BaseResponse<object>> SendEmailVerificationAsync(int userId);
    }
}