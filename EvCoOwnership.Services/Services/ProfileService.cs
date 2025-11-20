using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.UserDTOs;
using EvCoOwnership.Repositories.DTOs.ProfileDTOs;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace EvCoOwnership.Services.Services
{
    public class ProfileService : IProfileService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProfileService> _logger;

        public ProfileService(IUnitOfWork unitOfWork, ILogger<ProfileService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<BaseResponse<ProfileResponse>> GetProfileAsync(int userId, int requesterId)
        {
            try
            {
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<ProfileResponse>
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND",
                        Data = null
                    };
                }

                // Get basic statistics (simplified)
                var statistics = new ProfileStatistics
                {
                    TotalBookings = 0, // Would be calculated from bookings
                    CompletedTrips = 0,
                    TotalDistance = 0,
                    TotalSpent = 0,
                    VehiclesCoOwned = 0,
                    LastActivity = user.UpdatedAt,
                    DaysActive = user.CreatedAt.HasValue ? (DateTime.UtcNow - user.CreatedAt.Value).Days : 0,
                    CarbonFootprintSaved = 0
                };

                // Default settings
                var notificationSettings = new NotificationSettings();
                var privacySettings = new PrivacySettings();

                var response = new ProfileResponse
                {
                    UserId = user.Id,
                    Email = ShouldShowEmail(userId, requesterId) ? user.Email : "****@****.***",
                    FirstName = user.FirstName ?? "",
                    LastName = user.LastName ?? "",
                    Phone = ShouldShowPhone(userId, requesterId) ? user.Phone : "***-***-****",
                    Address = user.Address,
                    DateOfBirth = user.DateOfBirth?.ToDateTime(TimeOnly.MinValue),
                    AvatarUrl = user.ProfileImageUrl,
                    Bio = "", // Not available in current model
                    Role = user.RoleEnum?.ToString() ?? "",
                    CreatedAt = user.CreatedAt ?? DateTime.MinValue,
                    LastLoginAt = null, // Not available in current model
                    IsEmailVerified = true, // Assume verified for now
                    IsPhoneVerified = false, // Assume not verified for now
                    Statistics = statistics,
                    NotificationSettings = notificationSettings,
                    PrivacySettings = userId == requesterId ? privacySettings : new PrivacySettings()
                };

                return new BaseResponse<ProfileResponse>
                {
                    StatusCode = 200,
                    Message = "PROFILE_RETRIEVED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving profile for user {UserId}", userId);
                return new BaseResponse<ProfileResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        public async Task<BaseResponse<ProfileResponse>> UpdateProfileAsync(int userId, UpdateProfileRequest request)
        {
            try
            {
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<ProfileResponse>
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND",
                        Data = null
                    };
                }

                // Update user fields
                if (!string.IsNullOrEmpty(request.FirstName))
                    user.FirstName = request.FirstName;

                if (!string.IsNullOrEmpty(request.LastName))
                    user.LastName = request.LastName;

                if (!string.IsNullOrEmpty(request.Phone))
                    user.Phone = request.Phone;

                if (!string.IsNullOrEmpty(request.Address))
                    user.Address = request.Address;

                if (request.DateOfBirth.HasValue)
                    user.DateOfBirth = DateOnly.FromDateTime(request.DateOfBirth.Value);

                if (!string.IsNullOrEmpty(request.AvatarUrl))
                    user.ProfileImageUrl = request.AvatarUrl;

                user.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.UserRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();

                // Return updated profile
                return await GetProfileAsync(userId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
                return new BaseResponse<ProfileResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        public async Task<BaseResponse<object>> ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            try
            {
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND",
                        Data = null
                    };
                }

                // Verify current password (simplified)
                if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
                {
                    return new BaseResponse<object>
                    {
                        StatusCode = 400,
                        Message = "INVALID_CURRENT_PASSWORD",
                        Data = null
                    };
                }

                // Hash new password
                user.PasswordHash = HashPassword(request.NewPassword);
                user.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.UserRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();

                return new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "PASSWORD_CHANGED_SUCCESSFULLY",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", userId);
                return new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        public async Task<BaseResponse<NotificationSettings>> UpdateNotificationSettingsAsync(int userId, UpdateNotificationSettingsRequest request)
        {
            try
            {
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<NotificationSettings>
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND",
                        Data = null
                    };
                }

                // In a real implementation, notification settings would be stored in a separate table
                var settings = new NotificationSettings
                {
                    EmailNotifications = request.EmailNotifications,
                    PushNotifications = request.PushNotifications,
                    BookingReminders = request.BookingReminders,
                    MaintenanceAlerts = request.MaintenanceAlerts,
                    PaymentNotifications = request.PaymentNotifications,
                    SystemAnnouncements = request.SystemAnnouncements
                };

                return new BaseResponse<NotificationSettings>
                {
                    StatusCode = 200,
                    Message = "NOTIFICATION_SETTINGS_UPDATED_SUCCESSFULLY",
                    Data = settings
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification settings for user {UserId}", userId);
                return new BaseResponse<NotificationSettings>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        public async Task<BaseResponse<PrivacySettings>> UpdatePrivacySettingsAsync(int userId, UpdatePrivacySettingsRequest request)
        {
            try
            {
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<PrivacySettings>
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND",
                        Data = null
                    };
                }

                // In a real implementation, privacy settings would be stored in a separate table
                var settings = new PrivacySettings
                {
                    ProfileVisibility = request.ProfileVisibility,
                    ShowEmail = request.ShowEmail,
                    ShowPhone = request.ShowPhone,
                    ShareUsageData = request.ShareUsageData,
                    AllowDataAnalytics = request.AllowDataAnalytics
                };

                return new BaseResponse<PrivacySettings>
                {
                    StatusCode = 200,
                    Message = "PRIVACY_SETTINGS_UPDATED_SUCCESSFULLY",
                    Data = settings
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating privacy settings for user {UserId}", userId);
                return new BaseResponse<PrivacySettings>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        public async Task<BaseResponse<ActivityLogResponse>> GetActivityLogAsync(int userId, int page = 1, int pageSize = 50, string? category = null)
        {
            try
            {
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<ActivityLogResponse>
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND",
                        Data = null
                    };
                }

                // Simplified - in real implementation would query from activity log table
                var response = new ActivityLogResponse
                {
                    Activities = new List<ActivityLogItem>(),
                    TotalCount = 0,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalPages = 0
                };

                return new BaseResponse<ActivityLogResponse>
                {
                    StatusCode = 200,
                    Message = "ACTIVITY_LOG_RETRIEVED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving activity log for user {UserId}", userId);
                return new BaseResponse<ActivityLogResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        public async Task<BaseResponse<SecurityLogResponse>> GetSecurityLogAsync(int userId, int days = 30)
        {
            try
            {
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<SecurityLogResponse>
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND",
                        Data = null
                    };
                }

                // Simplified - in real implementation would query from security log table
                var response = new SecurityLogResponse
                {
                    SecurityEvents = new List<SecurityLogItem>(),
                    TotalCount = 0,
                    Summary = new SecuritySummary()
                };

                return new BaseResponse<SecurityLogResponse>
                {
                    StatusCode = 200,
                    Message = "SECURITY_LOG_RETRIEVED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving security log for user {UserId}", userId);
                return new BaseResponse<SecurityLogResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        public async Task<BaseResponse<object>> DeleteAccountAsync(int userId)
        {
            try
            {
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND",
                        Data = null
                    };
                }

                // For now, we'll just update the status instead of soft delete
                user.StatusEnum = Repositories.Enums.EUserStatus.Inactive;
                user.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.UserRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();

                return new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "ACCOUNT_DEACTIVATED_SUCCESSFULLY",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating account for user {UserId}", userId);
                return new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        public async Task<BaseResponse<ProfileResponse>> UpdateAvatarAsync(int userId, string avatarUrl)
        {
            try
            {
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<ProfileResponse>
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND",
                        Data = null
                    };
                }

                user.ProfileImageUrl = avatarUrl;
                user.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.UserRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();

                return await GetProfileAsync(userId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating avatar for user {UserId}", userId);
                return new BaseResponse<ProfileResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        public async Task<BaseResponse<object>> VerifyEmailAsync(int userId, string verificationToken)
        {
            // Simplified implementation
            await Task.Delay(1);
            return new BaseResponse<object>
            {
                StatusCode = 200,
                Message = "EMAIL_VERIFICATION_NOT_IMPLEMENTED",
                Data = null
            };
        }

        public async Task<BaseResponse<object>> SendEmailVerificationAsync(int userId)
        {
            // Simplified implementation
            await Task.Delay(1);
            return new BaseResponse<object>
            {
                StatusCode = 200,
                Message = "EMAIL_VERIFICATION_NOT_IMPLEMENTED",
                Data = null
            };
        }

        #region Private Helper Methods

        private bool ShouldShowEmail(int profileUserId, int requesterId)
        {
            // For now, only show full email to the profile owner
            return profileUserId == requesterId;
        }

        private bool ShouldShowPhone(int profileUserId, int requesterId)
        {
            // For now, only show full phone to the profile owner
            return profileUserId == requesterId;
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            var hashedInput = HashPassword(password);
            return hashedInput == hashedPassword;
        }

        #endregion
    }
}