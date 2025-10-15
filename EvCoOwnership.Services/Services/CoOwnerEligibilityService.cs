using EvCoOwnership.DTOs.AuthDTOs;
using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace EvCoOwnership.Services.Services
{
    /// <summary>
    /// Service for managing Co-owner eligibility and requirements
    /// Ensures only verified license holders can participate in vehicle co-ownership
    /// </summary>
    public class CoOwnerEligibilityService : ICoOwnerEligibilityService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILicenseVerificationService _licenseService;
        private readonly ILogger<CoOwnerEligibilityService> _logger;

        public CoOwnerEligibilityService(
            IUnitOfWork unitOfWork,
            ILicenseVerificationService licenseService,
            ILogger<CoOwnerEligibilityService> logger)
        {
            _unitOfWork = unitOfWork;
            _licenseService = licenseService;
            _logger = logger;
        }

        /// <summary>
        /// Checks if a user is eligible to become a Co-owner
        /// Requirements: Valid driving license, active account, age >= 18
        /// </summary>
        public async Task<BaseResponse> CheckCoOwnerEligibilityAsync(int userId)
        {
            try
            {
                var user = await _unitOfWork.UserRepository.GetUserWithRolesByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND"
                    };
                }

                var eligibilityChecks = new List<string>();
                var issues = new List<string>();

                // Check 1: Account status
                if (user.StatusEnum != Repositories.Enums.EUserStatus.Active)
                {
                    issues.Add("ACCOUNT_NOT_ACTIVE");
                }
                else
                {
                    eligibilityChecks.Add("ACCOUNT_STATUS_VALID");
                }

                // Check 2: Age requirement (18+ for co-ownership)
                if (user.DateOfBirth.HasValue)
                {
                    var age = DateTime.Now.Year - user.DateOfBirth.Value.Year;
                    if (user.DateOfBirth.Value > DateOnly.FromDateTime(DateTime.Now.AddYears(-age)))
                        age--;

                    if (age >= 18)
                    {
                        eligibilityChecks.Add("AGE_REQUIREMENT_MET");
                    }
                    else
                    {
                        issues.Add("MINIMUM_AGE_NOT_MET_18_REQUIRED");
                    }
                }
                else
                {
                    issues.Add("DATE_OF_BIRTH_REQUIRED");
                }

                // Check 3: Valid driving license
                var coOwner = await _unitOfWork.CoOwnerRepository.GetByIdAsync(userId);
                if (coOwner != null)
                {
                    var licenses = await _unitOfWork.DrivingLicenseRepository.GetByCoOwnerIdAsync(userId);
                    var validLicense = licenses.FirstOrDefault(l =>
                        !l.ExpiryDate.HasValue || l.ExpiryDate.Value > DateOnly.FromDateTime(DateTime.Now));

                    if (validLicense != null)
                    {
                        eligibilityChecks.Add("VALID_DRIVING_LICENSE");
                    }
                    else
                    {
                        issues.Add("NO_VALID_DRIVING_LICENSE");
                    }
                }
                else
                {
                    issues.Add("NO_DRIVING_LICENSE_REGISTERED");
                }

                var isEligible = !issues.Any();

                return new BaseResponse
                {
                    StatusCode = 200,
                    Message = isEligible ? "ELIGIBLE_FOR_CO_OWNERSHIP" : "NOT_ELIGIBLE_FOR_CO_OWNERSHIP",
                    Data = new
                    {
                        UserId = userId,
                        IsEligible = isEligible,
                        PassedChecks = eligibilityChecks,
                        Issues = issues,
                        RequiredActions = issues.Contains("NO_DRIVING_LICENSE_REGISTERED")
                            ? new[] { "REGISTER_DRIVING_LICENSE" }
                            : issues.Contains("NO_VALID_DRIVING_LICENSE")
                            ? new[] { "RENEW_DRIVING_LICENSE" }
                            : new string[0]
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Co-owner eligibility for user {UserId}", userId);
                return new BaseResponse
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        /// <summary>
        /// Promotes a user to Co-owner status after eligibility verification
        /// </summary>
        public async Task<BaseResponse> PromoteToCoOwnerAsync(int userId)
        {
            var eligibilityCheck = await CheckCoOwnerEligibilityAsync(userId);

            if (eligibilityCheck.StatusCode != 200)
                return eligibilityCheck;

            var eligibilityData = eligibilityCheck.Data as dynamic;
            if (eligibilityData?.IsEligible != true)
            {
                return new BaseResponse
                {
                    StatusCode = 400,
                    Message = "USER_NOT_ELIGIBLE_FOR_CO_OWNERSHIP",
                    Data = eligibilityCheck.Data
                };
            }

            try
            {
                var user = await _unitOfWork.UserRepository.GetUserWithRolesByIdAsync(userId);

                // Check if already a co-owner
                if (user?.RoleEnum == Repositories.Enums.EUserRole.CoOwner)
                {
                    return new BaseResponse
                    {
                        StatusCode = 409,
                        Message = "USER_ALREADY_CO_OWNER"
                    };
                }

                // Set Co-owner role
                if (user != null)
                {
                    user.RoleEnum = Repositories.Enums.EUserRole.CoOwner;
                    _unitOfWork.UserRepository.Update(user);

                    // Create CoOwner record if not exists
                    var existingCoOwner = await _unitOfWork.CoOwnerRepository.GetByIdAsync(userId);
                    if (existingCoOwner == null)
                    {
                        var newCoOwner = new Repositories.Models.CoOwner
                        {
                            UserId = userId,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _unitOfWork.CoOwnerRepository.Create(newCoOwner);
                    }

                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation("User {UserId} promoted to Co-owner status", userId);

                    return new BaseResponse
                    {
                        StatusCode = 200,
                        Message = "PROMOTION_TO_CO_OWNER_SUCCESS",
                        Data = new
                        {
                            UserId = userId,
                            NewRole = "CoOwner",
                            PromotedAt = DateTime.UtcNow
                        }
                    };
                }

                return new BaseResponse
                {
                    StatusCode = 500,
                    Message = "USER_NOT_FOUND"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error promoting user {UserId} to Co-owner", userId);
                return new BaseResponse
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        /// <summary>
        /// Gets Co-ownership statistics for reporting
        /// </summary>
        public async Task<BaseResponse> GetCoOwnershipStatsAsync()
        {
            try
            {
                var totalUsers = await _unitOfWork.UserRepository.GetAllAsync();
                var coOwners = totalUsers.Where(u => u.RoleEnum == Repositories.Enums.EUserRole.CoOwner).ToList();
                var totalLicenses = await _unitOfWork.DrivingLicenseRepository.GetAllAsync();

                var stats = new
                {
                    TotalUsers = totalUsers.Count,
                    TotalCoOwners = coOwners.Count,
                    CoOwnershipRate = totalUsers.Count > 0 ? (double)coOwners.Count / totalUsers.Count * 100 : 0,
                    TotalLicensesRegistered = totalLicenses.Count,
                    ActiveLicenses = totalLicenses.Count(l => !l.ExpiryDate.HasValue || l.ExpiryDate.Value > DateOnly.FromDateTime(DateTime.Now)),
                    ExpiredLicenses = totalLicenses.Count(l => l.ExpiryDate.HasValue && l.ExpiryDate.Value <= DateOnly.FromDateTime(DateTime.Now)),
                    ExpiringSoon = totalLicenses.Count(l => l.ExpiryDate.HasValue &&
                        l.ExpiryDate.Value > DateOnly.FromDateTime(DateTime.Now) &&
                        l.ExpiryDate.Value <= DateOnly.FromDateTime(DateTime.Now.AddDays(30)))
                };

                return new BaseResponse
                {
                    StatusCode = 200,
                    Message = "CO_OWNERSHIP_STATS_RETRIEVED",
                    Data = stats
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Co-ownership statistics");
                return new BaseResponse
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }
    }
}