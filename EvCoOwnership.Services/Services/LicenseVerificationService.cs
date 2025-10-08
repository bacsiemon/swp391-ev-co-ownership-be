using EvCoOwnership.DTOs.AuthDTOs;
using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Services.Interfaces;
using EvCoOwnership.Services.Mapping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace EvCoOwnership.Services.Services
{
    /// <summary>
    /// Service for driving license verification and management
    /// </summary>
    public class LicenseVerificationService : ILicenseVerificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LicenseVerificationService> _logger;

        // Mock database of valid license patterns by province/authority
        private static readonly Dictionary<string, List<string>> ValidLicensePatterns = new()
        {
            { "HO CHI MINH", new List<string> { @"^[0-9]{9}$", @"^B[0-9]{8}$" } },
            { "HA NOI", new List<string> { @"^[0-9]{9}$", @"^A[0-9]{8}$" } },
            { "DA NANG", new List<string> { @"^[0-9]{9}$", @"^C[0-9]{8}$" } },
            { "CAN THO", new List<string> { @"^[0-9]{9}$", @"^D[0-9]{8}$" } },
            // Add more provinces as needed
            { "DEFAULT", new List<string> { @"^[A-Z0-9]{6,15}$" } }
        };

        // Mock blacklist of invalid/suspended licenses
        private static readonly HashSet<string> BlacklistedLicenses = new()
        {
            "123456789", "SUSPENDED01", "REVOKED001"
        };

        public LicenseVerificationService(
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            ILogger<LicenseVerificationService> logger)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Verifies a driving license with the provided information
        /// </summary>
        public async Task<BaseResponse> VerifyLicenseAsync(VerifyLicenseRequest request)
        {
            try
            {
                _logger.LogInformation("Starting license verification for license number: {LicenseNumber}", request.LicenseNumber);

                // Check if license already exists in our system
                var existingLicense = await _unitOfWork.DrivingLicenseRepository
                    .GetByLicenseNumberAsync(request.LicenseNumber);

                if (existingLicense != null)
                {
                    _logger.LogWarning("License number {LicenseNumber} already exists in system", request.LicenseNumber);
                    return new BaseResponse
                    {
                        StatusCode = 409,
                        Message = "LICENSE_ALREADY_REGISTERED",
                        Data = new { LicenseNumber = request.LicenseNumber }
                    };
                }

                // Perform license verification
                var verificationResult = await PerformLicenseVerificationAsync(request);

                if (!verificationResult.IsValid)
                {
                    _logger.LogWarning("License verification failed for {LicenseNumber}: {Issues}",
                        request.LicenseNumber, string.Join(", ", verificationResult.Issues ?? new List<string>()));

                    return new BaseResponse
                    {
                        StatusCode = 400,
                        Message = "LICENSE_VERIFICATION_FAILED",
                        Data = verificationResult
                    };
                }

                _logger.LogInformation("License verification successful for {LicenseNumber}", request.LicenseNumber);

                return new BaseResponse
                {
                    StatusCode = 200,
                    Message = "LICENSE_VERIFICATION_SUCCESS",
                    Data = verificationResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during license verification for {LicenseNumber}", request.LicenseNumber);
                return new BaseResponse
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = new { Error = "An error occurred during license verification" }
                };
            }
        }

        /// <summary>
        /// Checks if a license number is already registered in the system
        /// </summary>
        public async Task<BaseResponse> CheckLicenseExistsAsync(string licenseNumber)
        {
            try
            {
                var existingLicense = await _unitOfWork.DrivingLicenseRepository
                    .GetByLicenseNumberAsync(licenseNumber);

                var exists = existingLicense != null;

                return new BaseResponse
                {
                    StatusCode = 200,
                    Message = exists ? "LICENSE_EXISTS" : "LICENSE_NOT_FOUND",
                    Data = new
                    {
                        LicenseNumber = licenseNumber,
                        Exists = exists,
                        RegisteredAt = existingLicense?.CreatedAt
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking license existence for {LicenseNumber}", licenseNumber);
                return new BaseResponse
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        /// <summary>
        /// Gets license information by license number (for authorized users)
        /// </summary>
        public async Task<BaseResponse> GetLicenseInfoAsync(string licenseNumber, int userId)
        {
            try
            {
                // Check if user has permission to view license info
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND"
                    };
                }

                var existingLicense = await _unitOfWork.DrivingLicenseRepository
                    .GetByLicenseNumberWithCoOwnerAsync(licenseNumber);

                if (existingLicense == null)
                {
                    return new BaseResponse
                    {
                        StatusCode = 404,
                        Message = "LICENSE_NOT_FOUND"
                    };
                }

                // Check if user has permission to view this license
                // (own license or admin/staff role)
                var userRoles = user.Roles?.Select(r => r.RoleNameEnum.ToString()).ToList() ?? new List<string>();
                var isOwner = existingLicense.CoOwner?.UserId == userId;
                var isAdmin = userRoles.Contains("Admin") || userRoles.Contains("Staff");

                if (!isOwner && !isAdmin)
                {
                    return new BaseResponse
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED"
                    };
                }

                var licenseInfo = existingLicense.ToLicenseDetails();

                return new BaseResponse
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = licenseInfo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting license info for {LicenseNumber}", licenseNumber);
                return new BaseResponse
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        /// <summary>
        /// Updates license status (for admin use)
        /// </summary>
        public async Task<BaseResponse> UpdateLicenseStatusAsync(string licenseNumber, string status, int adminUserId)
        {
            try
            {
                // Check if user is admin
                var adminUser = await _unitOfWork.UserRepository.GetUserWithRolesByIdAsync(adminUserId);
                if (adminUser == null)
                {
                    return new BaseResponse
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND"
                    };
                }

                var userRoles = adminUser.Roles?.Select(r => r.RoleNameEnum.ToString()).ToList() ?? new List<string>();
                if (!userRoles.Contains("Admin") && !userRoles.Contains("Staff"))
                {
                    return new BaseResponse
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED"
                    };
                }

                var existingLicense = await _unitOfWork.DrivingLicenseRepository
                    .GetByLicenseNumberAsync(licenseNumber);

                if (existingLicense == null)
                {
                    return new BaseResponse
                    {
                        StatusCode = 404,
                        Message = "LICENSE_NOT_FOUND"
                    };
                }

                // Update license (in a real system, you might have a status field)
                existingLicense.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.DrivingLicenseRepository.Update(existingLicense);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("License {LicenseNumber} status updated by admin {AdminUserId}",
                    licenseNumber, adminUserId);

                return new BaseResponse
                {
                    StatusCode = 200,
                    Message = "LICENSE_STATUS_UPDATED",
                    Data = new
                    {
                        LicenseNumber = licenseNumber,
                        NewStatus = status,
                        UpdatedBy = adminUserId,
                        UpdatedAt = existingLicense.UpdatedAt
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating license status for {LicenseNumber}", licenseNumber);
                return new BaseResponse
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        /// <summary>
        /// Performs the actual license verification logic
        /// </summary>
        private async Task<VerifyLicenseResponse> PerformLicenseVerificationAsync(VerifyLicenseRequest request)
        {
            var response = new VerifyLicenseResponse
            {
                VerifiedAt = DateTime.UtcNow,
                Issues = new List<string>()
            };

            // 1. Check if license is blacklisted
            if (BlacklistedLicenses.Contains(request.LicenseNumber))
            {
                response.IsValid = false;
                response.Status = "BLACKLISTED";
                response.Message = "License is suspended or revoked";
                response.Issues.Add("LICENSE_BLACKLISTED");
                return response;
            }

            // 2. Validate license format based on issuing authority
            if (!ValidateLicenseFormat(request.LicenseNumber, request.IssuedBy))
            {
                response.IsValid = false;
                response.Status = "INVALID_FORMAT";
                response.Message = "License format is invalid for the issuing authority";
                response.Issues.Add("INVALID_LICENSE_FORMAT");
                return response;
            }

            // 3. Validate issue date (cannot be in future, not too old)
            if (!ValidateIssueDate(request.IssueDate))
            {
                response.IsValid = false;
                response.Status = "INVALID_DATE";
                response.Message = "Issue date is invalid";
                response.Issues.Add("INVALID_ISSUE_DATE");
                return response;
            }

            // 4. Check age requirement (must be at least 18 years old when license was issued)
            if (!ValidateAgeRequirement(request.DateOfBirth, request.IssueDate))
            {
                response.IsValid = false;
                response.Status = "AGE_REQUIREMENT";
                response.Message = "Age requirement not met for license issuance";
                response.Issues.Add("AGE_REQUIREMENT_NOT_MET");
                return response;
            }

            // 5. Mock external API call for license verification
            // In a real implementation, this would call a government API
            await Task.Delay(100); // Simulate API call delay

            // 6. Generate mock license details if verification passes
            var licenseDetails = GenerateMockLicenseDetails(request);

            response.IsValid = true;
            response.Status = "VERIFIED";
            response.Message = "License verification successful";
            response.LicenseDetails = licenseDetails;

            return response;
        }

        /// <summary>
        /// Validates license format based on issuing authority
        /// </summary>
        private static bool ValidateLicenseFormat(string licenseNumber, string issuedBy)
        {
            var authority = issuedBy.ToUpperInvariant();

            // Get patterns for the specific authority or use default
            var patterns = ValidLicensePatterns.ContainsKey(authority)
                ? ValidLicensePatterns[authority]
                : ValidLicensePatterns["DEFAULT"];

            return patterns.Any(pattern => Regex.IsMatch(licenseNumber, pattern));
        }

        /// <summary>
        /// Validates if the issue date is reasonable
        /// </summary>
        private static bool ValidateIssueDate(DateOnly issueDate)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var earliestDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-50)); // Licenses older than 50 years are invalid

            return issueDate <= today && issueDate >= earliestDate;
        }

        /// <summary>
        /// Validates if the person was old enough to get a license when it was issued
        /// </summary>
        private static bool ValidateAgeRequirement(DateOnly dateOfBirth, DateOnly issueDate)
        {
            var ageAtIssue = issueDate.Year - dateOfBirth.Year;

            // Adjust for birthday not having occurred yet
            if (dateOfBirth > issueDate.AddYears(-ageAtIssue))
                ageAtIssue--;

            return ageAtIssue >= 18; // Minimum age requirement in Vietnam
        }

        /// <summary>
        /// Generates mock license details for successful verification
        /// </summary>
        private static LicenseDetails GenerateMockLicenseDetails(VerifyLicenseRequest request)
        {
            // Calculate expiry date (typically 10 years from issue date)
            var expiryDate = request.IssueDate.AddYears(10);
            var today = DateOnly.FromDateTime(DateTime.Now);

            // Determine status based on expiry date
            var status = expiryDate < today ? "EXPIRED" : "ACTIVE";

            return new LicenseDetails
            {
                LicenseNumber = request.LicenseNumber,
                HolderName = $"{request.FirstName} {request.LastName}",
                IssueDate = request.IssueDate,
                ExpiryDate = expiryDate,
                IssuedBy = request.IssuedBy,
                Status = status,
                LicenseClass = "B", // Mock class
                Restrictions = status == "ACTIVE" ? null : new List<string> { "LICENSE_EXPIRED" }
            };
        }
    }
}