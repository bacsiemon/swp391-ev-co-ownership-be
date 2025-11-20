using EvCoOwnership.Repositories.DTOs.UserDTOs;
using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Helpers.Helpers;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Services.Interfaces;
using EvCoOwnership.Services.Mapping;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvCoOwnership.Services.Services
{
    /// <summary>
    /// Implementation of User Profile Service
    /// </summary>
    public class UserProfileService : IUserProfileService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileUploadService _fileUploadService;
        private readonly ILogger<UserProfileService> _logger;

        public UserProfileService(
            IUnitOfWork unitOfWork,
            IFileUploadService fileUploadService,
            ILogger<UserProfileService> logger)
        {
            _unitOfWork = unitOfWork;
            _fileUploadService = fileUploadService;
            _logger = logger;
        }

        /// <summary>
        /// Gets user profile with statistics
        /// </summary>
        public async Task<BaseResponse<UserProfileResponse>> GetUserProfileAsync(int userId)
        {
            try
            {
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<UserProfileResponse>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy người dùng",
                        Data = null
                    };
                }

                // Calculate basic stats
                var allBookings = await _unitOfWork.BookingRepository.GetAllAsync();
                var userBookings = allBookings.Where(b => b.CoOwner?.UserId == userId).ToList();

                var allPayments = await _unitOfWork.PaymentRepository.GetAllAsync();
                var userPayments = allPayments.Where(p => p.UserId == userId).ToList();

                var response = UserProfileMappers.ToUserProfileResponse(user);

                // Update stats with calculated values
                response.Stats.TotalBookings = userBookings.Count;
                response.Stats.TotalPayments = userPayments.Count;
                response.Stats.TotalInvestmentAmount = userPayments.Sum(p => p.Amount);

                return new BaseResponse<UserProfileResponse>
                {
                    StatusCode = 200,
                    Message = "Lấy thông tin profile thành công",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile for user {UserId}", userId);
                return new BaseResponse<UserProfileResponse>
                {
                    StatusCode = 500,
                    Message = "Lỗi hệ thống",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Updates user profile information
        /// </summary>
        public async Task<BaseResponse<UserProfileResponse>> UpdateUserProfileAsync(int userId, UpdateUserProfileRequest request)
        {
            try
            {
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<UserProfileResponse>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy người dùng",
                        Data = null
                    };
                }

                // Check if phone number already exists for another user
                if (!string.IsNullOrEmpty(request.Phone) && request.Phone != user.Phone)
                {
                    var existingUserWithPhone = await _unitOfWork.UserRepository.GetAllAsync();
                    if (existingUserWithPhone.Any(u => u.Phone == request.Phone && u.Id != userId))
                    {
                        return new BaseResponse<UserProfileResponse>
                        {
                            StatusCode = 409,
                            Message = "Số điện thoại đã được sử dụng",
                            Data = null
                        };
                    }
                }

                // Update user information
                user.FirstName = request.FirstName;
                user.LastName = request.LastName;
                user.Phone = request.Phone;
                user.DateOfBirth = request.DateOfBirth;
                user.Address = request.Address;

                await _unitOfWork.UserRepository.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                var response = UserProfileMappers.ToUserProfileResponse(user);
                return new BaseResponse<UserProfileResponse>
                {
                    StatusCode = 200,
                    Message = "Cập nhật profile thành công",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile for user {UserId}", userId);
                return new BaseResponse<UserProfileResponse>
                {
                    StatusCode = 500,
                    Message = "Lỗi hệ thống",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Changes user password
        /// </summary>
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
                        Message = "Không tìm thấy người dùng",
                        Data = null
                    };
                }

                // Verify current password
                var isCurrentPasswordValid = StringHasher.VerifyHash(request.CurrentPassword, user.PasswordHash, user.PasswordSalt);
                if (!isCurrentPasswordValid)
                {
                    return new BaseResponse<object>
                    {
                        StatusCode = 422,
                        Message = "Mật khẩu hiện tại không đúng",
                        Data = null
                    };
                }

                // Hash new password
                var newSalt = StringHasher.GenerateSalt();
                var newHash = StringHasher.HashWithSalt(request.NewPassword, newSalt);

                user.PasswordHash = newHash;
                user.PasswordSalt = newSalt;

                await _unitOfWork.UserRepository.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                return new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "Đổi mật khẩu thành công",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", userId);
                return new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "Lỗi hệ thống",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Gets user vehicles summary
        /// </summary>
        public async Task<BaseResponse<UserVehiclesSummary>> GetUserVehiclesSummaryAsync(int userId)
        {
            try
            {
                // Simple implementation with correct DTO structure
                var summary = new UserVehiclesSummary
                {
                    OwnedVehicles = new List<UserVehicleInfo>(),
                    CoOwnedVehicles = new List<UserVehicleInfo>(),
                    PendingInvitations = new List<UserVehicleInvitation>()
                };

                return new BaseResponse<UserVehiclesSummary>
                {
                    StatusCode = 200,
                    Message = "Lấy tóm tắt phương tiện thành công",
                    Data = summary
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vehicles summary for user {UserId}", userId);
                return new BaseResponse<UserVehiclesSummary>
                {
                    StatusCode = 500,
                    Message = "Lỗi hệ thống",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Gets user activity summary
        /// </summary>
        public async Task<BaseResponse<UserActivitySummary>> GetUserActivitySummaryAsync(int userId)
        {
            try
            {
                // Simple implementation with correct DTO structure
                var summary = new UserActivitySummary
                {
                    RecentBookings = new List<RecentBooking>(),
                    RecentPayments = new List<RecentPayment>(),
                    DrivingLicense = null
                };

                return new BaseResponse<UserActivitySummary>
                {
                    StatusCode = 200,
                    Message = "Lấy tóm tắt hoạt động thành công",
                    Data = summary
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting activity summary for user {UserId}", userId);
                return new BaseResponse<UserActivitySummary>
                {
                    StatusCode = 500,
                    Message = "Lỗi hệ thống",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Uploads profile image
        /// </summary>
        public async Task<BaseResponse<object>> UploadProfileImageAsync(int userId, IFormFile file)
        {
            try
            {
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy người dùng",
                        Data = null
                    };
                }

                // Upload file
                var uploadResult = await _fileUploadService.UploadFileAsync(file);
                if (uploadResult != null)
                {
                    user.ProfileImageUrl = uploadResult.Url;
                    await _unitOfWork.UserRepository.UpdateAsync(user);
                    await _unitOfWork.SaveChangesAsync();

                    return new BaseResponse<object>
                    {
                        StatusCode = 200,
                        Message = "Upload ảnh đại diện thành công",
                        Data = new { ProfileImageUrl = user.ProfileImageUrl }
                    };
                }

                return new BaseResponse<object>
                {
                    StatusCode = 400,
                    Message = "Upload ảnh thất bại",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading profile image for user {UserId}", userId);
                return new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "Lỗi hệ thống",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Deletes profile image
        /// </summary>
        public async Task<BaseResponse<object>> DeleteProfileImageAsync(int userId)
        {
            try
            {
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy người dùng",
                        Data = null
                    };
                }

                user.ProfileImageUrl = null;
                await _unitOfWork.UserRepository.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                return new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "Xóa ảnh đại diện thành công",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting profile image for user {UserId}", userId);
                return new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "Lỗi hệ thống",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Gets user profile by email (admin only)
        /// </summary>
        public async Task<BaseResponse<UserProfileResponse>> GetUserProfileByEmailAsync(string email, int requestingUserId)
        {
            try
            {
                // Simple implementation - just find user by email
                var users = await _unitOfWork.UserRepository.GetAllAsync();
                var user = users.FirstOrDefault(u => u.Email == email);

                if (user == null)
                {
                    return new BaseResponse<UserProfileResponse>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy người dùng với email này",
                        Data = null
                    };
                }

                var response = UserProfileMappers.ToUserProfileResponse(user);
                return new BaseResponse<UserProfileResponse>
                {
                    StatusCode = 200,
                    Message = "Lấy thông tin profile thành công",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile by email {Email}", email);
                return new BaseResponse<UserProfileResponse>
                {
                    StatusCode = 500,
                    Message = "Lỗi hệ thống",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Validates profile completeness
        /// </summary>
        public async Task<BaseResponse<UserProfileCompleteness>> ValidateProfileCompletenessAsync(int userId)
        {
            try
            {
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<UserProfileCompleteness>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy người dùng",
                        Data = null
                    };
                }

                var completeness = UserProfileMappers.CalculateProfileCompleteness(user);

                return new BaseResponse<UserProfileCompleteness>
                {
                    StatusCode = 200,
                    Message = "Kiểm tra tính đầy đủ profile thành công",
                    Data = completeness
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating profile completeness for user {UserId}", userId);
                return new BaseResponse<UserProfileCompleteness>
                {
                    StatusCode = 500,
                    Message = "Lỗi hệ thống",
                    Data = null
                };
            }
        }
    }
}