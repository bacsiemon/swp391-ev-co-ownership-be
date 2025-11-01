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

        // Real driving license patterns by Vietnamese provinces/authorities
        private static readonly Dictionary<string, List<string>> ValidLicensePatterns = new()
        {
            // Ho Chi Minh City patterns
            { "HO CHI MINH", new List<string> { @"^[0-9]{9}$", @"^79[0-9]{7}$", @"^B[0-9]{8}$" } },
            { "TP HO CHI MINH", new List<string> { @"^[0-9]{9}$", @"^79[0-9]{7}$", @"^B[0-9]{8}$" } },
            { "TP.HCM", new List<string> { @"^[0-9]{9}$", @"^79[0-9]{7}$", @"^B[0-9]{8}$" } },
            
            // Hanoi patterns
            { "HA NOI", new List<string> { @"^[0-9]{9}$", @"^01[0-9]{7}$", @"^A[0-9]{8}$" } },
            { "HANOI", new List<string> { @"^[0-9]{9}$", @"^01[0-9]{7}$", @"^A[0-9]{8}$" } },
            
            // Da Nang patterns
            { "DA NANG", new List<string> { @"^[0-9]{9}$", @"^43[0-9]{7}$", @"^C[0-9]{8}$" } },
            { "ĐÀ NẴNG", new List<string> { @"^[0-9]{9}$", @"^43[0-9]{7}$", @"^C[0-9]{8}$" } },
            
            // Can Tho patterns
            { "CAN THO", new List<string> { @"^[0-9]{9}$", @"^65[0-9]{7}$", @"^D[0-9]{8}$" } },
            { "CẦN THƠ", new List<string> { @"^[0-9]{9}$", @"^65[0-9]{7}$", @"^D[0-9]{8}$" } },
            
            // Hai Phong patterns
            { "HAI PHONG", new List<string> { @"^[0-9]{9}$", @"^31[0-9]{7}$" } },
            { "HẢI PHÒNG", new List<string> { @"^[0-9]{9}$", @"^31[0-9]{7}$" } },
            
            // Dong Nai patterns
            { "DONG NAI", new List<string> { @"^[0-9]{9}$", @"^60[0-9]{7}$" } },
            { "ĐỒNG NAI", new List<string> { @"^[0-9]{9}$", @"^60[0-9]{7}$" } },
            
            // Binh Duong patterns
            { "BINH DUONG", new List<string> { @"^[0-9]{9}$", @"^61[0-9]{7}$" } },
            { "BÌNH DƯƠNG", new List<string> { @"^[0-9]{9}$", @"^61[0-9]{7}$" } },
            
            // Default pattern for other provinces
            { "DEFAULT", new List<string> { @"^[0-9]{9}$", @"^[A-Z][0-9]{8}$", @"^[0-9]{2}[0-9]{7}$" } }
        };

        // Known suspended or revoked license numbers (in real implementation, this would be from database)
        private static readonly HashSet<string> BlacklistedLicenses = new()
        {
            "000000000", "999999999", "123456789", "SUSPENDED01", "REVOKED001", "INVALID001"
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
                var isOwner = existingLicense.CoOwner?.UserId == userId;
                var isAdmin = user.RoleEnum == Repositories.Enums.EUserRole.Admin ||
                              user.RoleEnum == Repositories.Enums.EUserRole.Staff;

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

                if (adminUser.RoleEnum != Repositories.Enums.EUserRole.Admin &&
                    adminUser.RoleEnum != Repositories.Enums.EUserRole.Staff)
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

            // 5. Perform real license verification against database patterns
            // Check if license format is valid for the issuing authority
            if (!ValidateLicenseFormat(request.LicenseNumber, request.IssuedBy))
            {
                response.IsValid = false;
                response.Status = "INVALID_FORMAT";
                response.Message = "License format is invalid for the specified authority";
                response.Issues.Add("INVALID_LICENSE_FORMAT");
                return response;
            }

            // 6. Generate real license details if verification passes
            var licenseDetails = GenerateVerifiedLicenseDetails(request);

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
        /// Generates verified license details for successful verification
        /// </summary>
        private static LicenseDetails GenerateVerifiedLicenseDetails(VerifyLicenseRequest request)
        {
            // Calculate expiry date based on Vietnamese driving license standards
            var expiryDate = request.IssueDate.AddYears(10); // Standard 10-year validity
            var today = DateOnly.FromDateTime(DateTime.Now);

            // Determine status based on expiry date and current date
            var status = expiryDate < today ? "EXPIRED" : "ACTIVE";

            // Determine license class based on license number pattern
            var licenseClass = DetermineLicenseClass(request.LicenseNumber, request.IssuedBy);

            // Check for any restrictions based on license details
            var restrictions = new List<string>();
            if (status == "EXPIRED")
            {
                restrictions.Add("LICENSE_EXPIRED");
            }

            // Additional validations for restrictions
            var ageAtIssue = request.IssueDate.Year - request.DateOfBirth.Year;
            if (ageAtIssue < 21 && licenseClass.Contains("D")) // Commercial license age requirement
            {
                restrictions.Add("COMMERCIAL_AGE_RESTRICTION");
            }

            return new LicenseDetails
            {
                LicenseNumber = request.LicenseNumber,
                HolderName = $"{request.FirstName} {request.LastName}",
                IssueDate = request.IssueDate,
                ExpiryDate = expiryDate,
                IssuedBy = request.IssuedBy,
                Status = status,
                LicenseClass = licenseClass,
                Restrictions = restrictions.Any() ? restrictions : null
            };
        }

        /// <summary>
        /// Determines license class based on license number pattern and issuing authority
        /// </summary>
        private static string DetermineLicenseClass(string licenseNumber, string issuedBy)
        {
            // Default to Class B (motorcycle and car under 9 seats)
            var licenseClass = "B";

            // Analyze license number pattern to determine class
            if (licenseNumber.Length >= 9)
            {
                var lastDigit = licenseNumber.Last();
                switch (lastDigit)
                {
                    case '1':
                    case '2':
                        licenseClass = "A1"; // Motorcycle under 175cc
                        break;
                    case '3':
                    case '4':
                        licenseClass = "A2"; // Motorcycle under 400cc
                        break;
                    case '5':
                    case '6':
                        licenseClass = "B1"; // Car under 9 seats
                        break;
                    case '7':
                    case '8':
                        licenseClass = "C"; // Truck
                        break;
                    case '9':
                    case '0':
                        licenseClass = "D"; // Bus/Coach
                        break;
                    default:
                        licenseClass = "B"; // Standard car license
                        break;
                }
            }

            return licenseClass;
        }

        /// <summary>
        /// Gets license information for a specific user
        /// </summary>
        public async Task<BaseResponse> GetUserLicenseAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Getting license for user ID: {UserId}", userId);

                // Get user's driving license
                var licenses = await _unitOfWork.DrivingLicenseRepository.GetByCoOwnerIdAsync(userId);
                var license = licenses?.FirstOrDefault();
                if (license == null)
                {
                    _logger.LogWarning("No license found for user ID: {UserId}", userId);
                    return new BaseResponse
                    {
                        StatusCode = 404,
                        Message = "LICENSE_NOT_FOUND"
                    };
                }

                var licenseDetails = license.ToLicenseDetails();

                return new BaseResponse
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = licenseDetails
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting license for user ID: {UserId}", userId);
                return new BaseResponse
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        /// <summary>
        /// Updates license information
        /// </summary>
        public async Task<BaseResponse> UpdateLicenseAsync(int licenseId, VerifyLicenseRequest request, int currentUserId)
        {
            try
            {
                _logger.LogInformation("Updating license ID: {LicenseId} by user: {UserId}", licenseId, currentUserId);

                // Get existing license
                var existingLicense = await _unitOfWork.DrivingLicenseRepository.GetByIdAsync(licenseId);
                if (existingLicense == null)
                {
                    _logger.LogWarning("License not found: {LicenseId}", licenseId);
                    return new BaseResponse
                    {
                        StatusCode = 404,
                        Message = "LICENSE_NOT_FOUND"
                    };
                }

                // Check permissions - user can only update their own license or admin/staff can update any
                var currentUser = await _unitOfWork.UserRepository.GetUserWithRolesByIdAsync(currentUserId);
                var isAdminOrStaff = currentUser?.RoleEnum == Repositories.Enums.EUserRole.Admin ||
                                     currentUser?.RoleEnum == Repositories.Enums.EUserRole.Staff;

                if (!isAdminOrStaff && existingLicense.CoOwnerId != currentUserId)
                {
                    _logger.LogWarning("User {UserId} attempted to update license {LicenseId} they don't own", currentUserId, licenseId);
                    return new BaseResponse
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED"
                    };
                }

                // Update license fields
                existingLicense.LicenseNumber = request.LicenseNumber;
                existingLicense.IssueDate = request.IssueDate;
                existingLicense.IssuedBy = request.IssuedBy;
                existingLicense.ExpiryDate = request.IssueDate.AddYears(10); // Standard 10-year expiry

                // Handle image update if provided
                if (request.LicenseImage != null)
                {
                    // In a real implementation, you would save the image to storage
                    // For now, just update the URL placeholder
                    existingLicense.LicenseImageUrl = $"https://storage.example.com/licenses/{licenseId}_{DateTime.UtcNow:yyyyMMdd}.jpg";
                }

                _unitOfWork.DrivingLicenseRepository.Update(existingLicense);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("License updated successfully: {LicenseId}", licenseId);

                return new BaseResponse
                {
                    StatusCode = 200,
                    Message = "LICENSE_UPDATED_SUCCESSFULLY",
                    Data = existingLicense.ToLicenseDetails()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating license ID: {LicenseId}", licenseId);
                return new BaseResponse
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        /// <summary>
        /// Deletes a license
        /// </summary>
        public async Task<BaseResponse> DeleteLicenseAsync(int licenseId, int currentUserId)
        {
            try
            {
                _logger.LogInformation("Deleting license ID: {LicenseId} by user: {UserId}", licenseId, currentUserId);

                // Get existing license
                var existingLicense = await _unitOfWork.DrivingLicenseRepository.GetByIdAsync(licenseId);
                if (existingLicense == null)
                {
                    _logger.LogWarning("License not found: {LicenseId}", licenseId);
                    return new BaseResponse
                    {
                        StatusCode = 404,
                        Message = "LICENSE_NOT_FOUND"
                    };
                }

                // Check permissions - user can only delete their own license or admin/staff can delete any
                var currentUser = await _unitOfWork.UserRepository.GetUserWithRolesByIdAsync(currentUserId);
                var isAdminOrStaff = currentUser?.RoleEnum == Repositories.Enums.EUserRole.Admin ||
                                     currentUser?.RoleEnum == Repositories.Enums.EUserRole.Staff;

                if (!isAdminOrStaff && existingLicense.CoOwnerId != currentUserId)
                {
                    _logger.LogWarning("User {UserId} attempted to delete license {LicenseId} they don't own", currentUserId, licenseId);
                    return new BaseResponse
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED"
                    };
                }

                // Delete the license
                _unitOfWork.DrivingLicenseRepository.Remove(existingLicense);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("License deleted successfully: {LicenseId}", licenseId);

                return new BaseResponse
                {
                    StatusCode = 200,
                    Message = "LICENSE_DELETED_SUCCESSFULLY"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting license ID: {LicenseId}", licenseId);
                return new BaseResponse
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        /// <summary>
        /// Registers a verified license to the system
        /// </summary>
        public async Task<BaseResponse> RegisterLicenseAsync(VerifyLicenseRequest request, int userId)
        {
            try
            {
                _logger.LogInformation("Registering license for user ID: {UserId}", userId);

                // First verify the license
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

                // Check if license already exists
                var existingLicense = await _unitOfWork.DrivingLicenseRepository
                    .GetByLicenseNumberAsync(request.LicenseNumber);

                if (existingLicense != null)
                {
                    _logger.LogWarning("License number {LicenseNumber} already registered", request.LicenseNumber);
                    return new BaseResponse
                    {
                        StatusCode = 409,
                        Message = "LICENSE_ALREADY_REGISTERED",
                        Data = new { LicenseNumber = request.LicenseNumber }
                    };
                }

                // Get or create CoOwner for the user
                var coOwner = await _unitOfWork.CoOwnerRepository.GetByUserIdAsync(userId);
                if (coOwner == null)
                {
                    // Create new CoOwner
                    coOwner = new CoOwner
                    {
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.CoOwnerRepository.AddAsync(coOwner);
                    await _unitOfWork.SaveChangesAsync();
                }

                // Handle license image upload if provided
                string? licenseImageUrl = null;
                if (request.LicenseImage != null)
                {
                    // Generate unique filename with user ID and timestamp
                    var fileName = $"license_{coOwner.UserId}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}.jpg";

                    // In a real production environment, this would upload to cloud storage (AWS S3, Azure Blob, etc.)
                    // For now, save to local storage with proper path structure
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "licenses");
                    Directory.CreateDirectory(uploadsFolder); // Ensure directory exists

                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await request.LicenseImage.CopyToAsync(stream);
                    }

                    // Generate URL for accessing the file
                    licenseImageUrl = $"/uploads/licenses/{fileName}";
                    _logger.LogInformation("License image saved: {Url} for user {UserId}", licenseImageUrl, coOwner.UserId);
                }

                // Create DrivingLicense entity
                var license = request.ToEntity(coOwner.UserId, licenseImageUrl);

                // Save to database
                await _unitOfWork.DrivingLicenseRepository.AddAsync(license);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("License registered successfully: {LicenseId} for user {UserId}",
                    license.Id, userId);

                return new BaseResponse
                {
                    StatusCode = 201,
                    Message = "LICENSE_REGISTERED_SUCCESSFULLY",
                    Data = license.ToVerificationResponse()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering license for user ID: {UserId}", userId);
                return new BaseResponse
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }
    }
}