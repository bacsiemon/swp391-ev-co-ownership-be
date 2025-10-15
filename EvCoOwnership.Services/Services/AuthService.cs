using EvCoOwnership.DTOs.AuthDTOs;
using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Helpers.Helpers;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Services.Interfaces;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Services.Mapping;
using EvCoOwnership.Services.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvCoOwnership.Services.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;

        public AuthService(IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }
        private string RESET_PASSWORD_SUFFIX = "_RESET_PASS";

        /// <summary>
        /// Authenticates user with email and password
        /// </summary>
        /// <param name="request">Login request containing email and password</param>
        /// <returns>BaseResponse with status 200 (success), 400 (invalid credentials), 403 (account suspended)</returns>
        public async Task<BaseResponse> LoginAsync(LoginRequest request)
        {
            // Get user with roles by email
            var user = await _unitOfWork.UserRepository.GetUserWithRolesByEmailAsync(request.Email);
            if (user == null)
            {
                return new BaseResponse
                {
                    StatusCode = 400,
                    Message = "INVALID_EMAIL_OR_PASSWORD",
                };
            }

            // Verify password
            if (!StringHasher.VerifyHash(request.Password, user.PasswordHash, user.PasswordSalt))
            {
                return new BaseResponse
                {
                    StatusCode = 400,
                    Message = "INVALID_EMAIL_OR_PASSWORD",
                };
            }

            // Check if user is active
            if (user.StatusEnum == EUserStatus.Suspended)
            {
                return new BaseResponse
                {
                    StatusCode = 403,
                    Message = "ACCOUNT_SUSPENDED",
                };
            }

            if (user.StatusEnum == EUserStatus.Inactive)
            {
                return new BaseResponse
                {
                    StatusCode = 403,
                    Message = "ACCOUNT_INACTIVE",
                };
            }

            // Get user role
            var roles = user.RoleEnum.HasValue ? new List<string> { user.RoleEnum.ToString() } : new List<string>();

            // Generate tokens
            var userWrapper = new JwtUserDataWrapper(user);
            var accessToken = JwtHelper.GenerateAccessToken(userWrapper, roles, _configuration);
            var refreshToken = JwtHelper.GenerateRefreshToken();
            var accessTokenExpires = JwtHelper.GetAccessTokenExpiration(_configuration);
            var refreshTokenExpires = JwtHelper.GetRefreshTokenExpiration(_configuration);

            // Save or update refresh token
            var existingRefreshToken = await _unitOfWork.UserRefreshTokenRepository.GetByUserIdAsync(user.Id);
            if (existingRefreshToken != null)
            {
                existingRefreshToken.RefreshToken = refreshToken;
                existingRefreshToken.ExpiresAt = refreshTokenExpires;
                _unitOfWork.UserRefreshTokenRepository.Update(existingRefreshToken);
            }
            else
            {
                var newRefreshToken = new UserRefreshToken
                {
                    UserId = user.Id,
                    RefreshToken = refreshToken,
                    ExpiresAt = refreshTokenExpires
                };
                _unitOfWork.UserRefreshTokenRepository.Create(newRefreshToken);
            }

            await _unitOfWork.SaveChangesAsync();

            // Create response using mapper extension method
            var loginResponse = user.ToLoginResponse(accessToken, refreshToken, accessTokenExpires, refreshTokenExpires);

            return new BaseResponse
            {
                StatusCode = 200,
                Message = "LOGIN_SUCCESS",
                Data = loginResponse
            };
        }

        /// <summary>
        /// Registers a new user account
        /// </summary>
        /// <param name="request">Registration request containing user details</param>
        /// <returns>BaseResponse with status 201 (created), 409 (email exists)</returns>
        public async Task<BaseResponse> RegisterAsync(RegisterRequest request)
        {
            // Check if email already exists
            if (await _unitOfWork.UserRepository.EmailExistsAsync(request.Email))
            {
                return new BaseResponse
                {
                    StatusCode = 409,
                    Message = "EMAIL_ALREADY_EXISTS",
                };
            }

            // Generate salt and hash password
            var salt = StringHasher.GenerateSalt();
            var passwordHash = StringHasher.HashWithSalt(request.Password, salt);

            // Create new user using mapper extension method
            var user = request.ToEntity(passwordHash, salt);

            _unitOfWork.UserRepository.Create(user);
            await _unitOfWork.SaveChangesAsync();

            // Create Co-Owner record
            var coOwner = new CoOwner
            {
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _unitOfWork.CoOwnerRepository.Create(coOwner);
            await _unitOfWork.SaveChangesAsync();

            return new BaseResponse
            {
                StatusCode = 201,
                Message = "REGISTRATION_SUCCESS",
                Data = user.ToRegistrationResponse()
            };
        }

        /// <summary>
        /// Refreshes the access token using a valid refresh token
        /// </summary>
        /// <param name="request">Refresh token request</param>
        /// <returns>BaseResponse with status 200 (success), 401 (invalid/expired token), 404 (user not found)</returns>
        public async Task<BaseResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            // Get refresh token from database
            var userRefreshToken = await _unitOfWork.UserRefreshTokenRepository.GetByRefreshTokenAsync(request.RefreshToken);
            if (userRefreshToken == null || userRefreshToken.ExpiresAt < DateTime.UtcNow)
            {
                return new BaseResponse
                {
                    StatusCode = 401,
                    Message = "INVALID_OR_EXPIRED_REFRESH_TOKEN",
                };
            }

            // Get user with roles
            var user = await _unitOfWork.UserRepository.GetUserWithRolesAsync(userRefreshToken.UserId);
            if (user == null)
            {
                return new BaseResponse
                {
                    StatusCode = 404,
                    Message = "USER_NOT_FOUND",
                };
            }

            // Check if user is active
            if (user.StatusEnum == EUserStatus.Suspended)
            {
                return new BaseResponse
                {
                    StatusCode = 403,
                    Message = "ACCOUNT_SUSPENDED",
                };
            }

            if (user.StatusEnum == EUserStatus.Inactive)
            {
                return new BaseResponse
                {
                    StatusCode = 403,
                    Message = "ACCOUNT_INACTIVE",
                };
            }

            // Get user role
            var roles = user.RoleEnum.HasValue ? new List<string> { user.RoleEnum.Value.ToString() } : new List<string>();

            // Generate new tokens
            var userWrapper = new JwtUserDataWrapper(user);
            var newAccessToken = JwtHelper.GenerateAccessToken(userWrapper, roles, _configuration);
            var newRefreshToken = JwtHelper.GenerateRefreshToken();
            var accessTokenExpires = JwtHelper.GetAccessTokenExpiration(_configuration);
            var refreshTokenExpires = JwtHelper.GetRefreshTokenExpiration(_configuration);

            // Update refresh token
            userRefreshToken.RefreshToken = newRefreshToken;
            userRefreshToken.ExpiresAt = refreshTokenExpires;
            _unitOfWork.UserRefreshTokenRepository.Update(userRefreshToken);

            await _unitOfWork.SaveChangesAsync();

            // Create response using mapper extension method
            var loginResponse = user.ToLoginResponse(newAccessToken, newRefreshToken, accessTokenExpires, refreshTokenExpires);

            return new BaseResponse
            {
                StatusCode = 200,
                Message = "TOKEN_REFRESH_SUCCESS",
                Data = loginResponse
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="email">Email</param>
        /// <returns>BaseResponse with status 200, 404</returns>
        public async Task<BaseResponse> ForgotPasswordAsync(string email)
        {
            var user = await _unitOfWork.UserRepository.GetByEmailAsync(email);
            if (user == null)
                return new BaseResponse
                {
                    StatusCode = 404,
                    Message = "USER_NOT_FOUND",
                };
            OtpHelper.GenerateOtp(email + RESET_PASSWORD_SUFFIX);
            return new BaseResponse
            {
                StatusCode = 200,
                Message = "SUCCESS",
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns>BaseResponse with status 200, 400, 404</returns>
        public async Task<BaseResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _unitOfWork.UserRepository.GetByEmailAsync(request.Email);
            if (user == null)
                return new BaseResponse
                {
                    StatusCode = 404,
                    Message = "USER_NOT_FOUND",
                };
            if (!OtpHelper.VerifyOtp(request.Email + RESET_PASSWORD_SUFFIX, request.Otp))
            {
                return new BaseResponse
                {
                    StatusCode = 400,
                    Message = "INVALID_OTP",
                };
            }
            user.PasswordHash = StringHasher.HashWithSalt(request.NewPassword, user.PasswordSalt);
            OtpHelper.RemoveOtp(request.Email + RESET_PASSWORD_SUFFIX);
            _unitOfWork.UserRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();
            return new BaseResponse
            {
                StatusCode = 200,
                Message = "PASSWORD_RESET_SUCCESS",
            };
        }

        /// <summary>
        /// Verifies a driving license and registers it to a user account
        /// This is a critical step for Co-owner eligibility in the EV Co-ownership system
        /// </summary>
        /// <param name="request">License verification request</param>
        /// <returns>BaseResponse with verification result</returns>
        public async Task<BaseResponse> VerifyLicenseAsync(VerifyLicenseRequest request)
        {
            // This method provides a simplified license verification through AuthService
            // For more advanced features, use the dedicated LicenseVerificationService

            try
            {
                // Check if license already exists
                var existingLicense = await _unitOfWork.DrivingLicenseRepository
                    .GetByLicenseNumberAsync(request.LicenseNumber);

                if (existingLicense != null)
                {
                    return new BaseResponse
                    {
                        StatusCode = 409,
                        Message = "LICENSE_ALREADY_REGISTERED",
                        Data = new { LicenseNumber = request.LicenseNumber }
                    };
                }

                // Basic validation
                if (!IsValidLicenseFormat(request.LicenseNumber))
                {
                    return new BaseResponse
                    {
                        StatusCode = 400,
                        Message = "INVALID_LICENSE_FORMAT"
                    };
                }

                // Mock verification success
                var verificationResponse = new VerifyLicenseResponse
                {
                    IsValid = true,
                    Status = "VERIFIED",
                    Message = "License verification successful",
                    VerifiedAt = DateTime.UtcNow,
                    LicenseDetails = new LicenseDetails
                    {
                        LicenseNumber = request.LicenseNumber,
                        HolderName = $"{request.FirstName} {request.LastName}",
                        IssueDate = request.IssueDate,
                        ExpiryDate = request.IssueDate.AddYears(10),
                        IssuedBy = request.IssuedBy,
                        Status = "ACTIVE",
                        LicenseClass = "B"
                    }
                };

                return new BaseResponse
                {
                    StatusCode = 200,
                    Message = "LICENSE_VERIFICATION_SUCCESS",
                    Data = verificationResponse
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = new { Error = ex.Message }
                };
            }
        }

        /// <summary>
        /// Validates license format (basic check)
        /// </summary>
        /// <param name="licenseNumber">License number to validate</param>
        /// <returns>True if format is valid</returns>
        private static bool IsValidLicenseFormat(string licenseNumber)
        {
            if (string.IsNullOrEmpty(licenseNumber))
                return false;

            // Basic Vietnamese license format validation
            var patterns = new[]
            {
                @"^[0-9]{9}$",           // 9 digits
                @"^[A-Z][0-9]{8}$",     // 1 letter + 8 digits
                @"^[0-9]{12}$"          // 12 digits (new format)
            };

            return patterns.Any(pattern => System.Text.RegularExpressions.Regex.IsMatch(licenseNumber, pattern));
        }

        #region Development Only
        public BaseResponse GetForgotPasswordOtpAsync(string email)
        {
            var otp = OtpHelper.GetOtpData(email + RESET_PASSWORD_SUFFIX);
            return new BaseResponse
            {
                StatusCode = otp != null ? 200 : 404,
                Message = otp != null ? "OTP_FOUND" : "OTP_NOT_FOUND",
                Data = otp
            };
        }
        #endregion
    }
}