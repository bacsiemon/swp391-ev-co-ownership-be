using EvCoOwnership.DTOs.UserDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Controller for managing user profiles and profile-related operations
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IUserProfileService _userProfileService;
        private readonly ILogger<ProfileController> _logger;

        /// <summary>
        /// Initializes a new instance of the ProfileController
        /// </summary>
        public ProfileController(IUserProfileService userProfileService, ILogger<ProfileController> logger)
        {
            _userProfileService = userProfileService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the current user's profile information
        /// </summary>
        /// <response code="200">Profile retrieved successfully. Possible messages:  
        /// - USER_PROFILE_RETRIEVED_SUCCESSFULLY  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="404">User not found. Possible messages:  
        /// - USER_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Retrieves comprehensive profile information for the authenticated user including:  
        /// - Basic information (name, email, phone, address)  
        /// - Profile statistics (vehicles owned, bookings, payments)  
        /// - Role and status information  
        /// - Account creation and update dates  
        /// </remarks>
        [HttpGet]
        public async Task<IActionResult> GetMyProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _userProfileService.GetUserProfileAsync(userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets user profile by user ID (Admin/Staff only)
        /// </summary>
        /// <param name="userId">Target user ID</param>
        /// <response code="200">Profile retrieved successfully. Possible messages:  
        /// - USER_PROFILE_RETRIEVED_SUCCESSFULLY  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - admin/staff role required</response>
        /// <response code="404">User not found. Possible messages:  
        /// - USER_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Allows administrators and staff to view any user's profile information.  
        /// Regular users can only view their own profile using the GET /api/profile endpoint.
        /// </remarks>
        [HttpGet("{userId:int}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetUserProfile(int userId)
        {
            var response = await _userProfileService.GetUserProfileAsync(userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets user profile by email (Admin/Staff only)
        /// </summary>
        /// <param name="email">User email</param>
        /// <response code="200">Profile retrieved successfully. Possible messages:  
        /// - USER_PROFILE_RETRIEVED_SUCCESSFULLY  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - admin/staff role required</response>
        /// <response code="404">User not found. Possible messages:  
        /// - USER_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Allows administrators and staff to lookup user profiles by email address.
        /// </remarks>
        [HttpGet("by-email")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetUserProfileByEmail([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { Message = "EMAIL_REQUIRED" });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var requestingUserId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _userProfileService.GetUserProfileByEmailAsync(email, requestingUserId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => Forbid(response.Message),
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Updates the current user's profile information
        /// </summary>
        /// <param name="request">Profile update request</param>
        /// <response code="200">Profile updated successfully. Possible messages:  
        /// - USER_PROFILE_UPDATED_SUCCESSFULLY  
        /// </response>
        /// <response code="400">Validation error. Possible messages:  
        /// - FIRST_NAME_REQUIRED  
        /// - LAST_NAME_REQUIRED  
        /// - FIRST_NAME_MAX_50_CHARACTERS  
        /// - LAST_NAME_MAX_50_CHARACTERS  
        /// - FIRST_NAME_ONLY_LETTERS_AND_SPACES  
        /// - LAST_NAME_ONLY_LETTERS_AND_SPACES  
        /// - INVALID_VIETNAM_PHONE_FORMAT  
        /// - MUST_BE_AT_LEAST_18_YEARS_OLD  
        /// - ADDRESS_MAX_200_CHARACTERS  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="404">User not found. Possible messages:  
        /// - USER_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Updates user profile information including name, phone, date of birth, and address.  
        /// 
        /// **Validation Rules:**  
        /// - First/Last Name: Required, max 50 chars, only letters and spaces  
        /// - Phone: Optional, must match Vietnam format (+84 or 0 followed by valid mobile number)  
        /// - Date of Birth: Optional, user must be at least 18 years old  
        /// - Address: Optional, max 200 characters  
        /// - Profile Image URL: Optional, should be valid URL to uploaded image  
        /// 
        /// **Vietnam Phone Format Examples:**  
        /// - +84912345678  
        /// - 0912345678  
        /// - 0387654321  
        /// </remarks>
        [HttpPut]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserProfileRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _userProfileService.UpdateUserProfileAsync(userId, request);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Changes the current user's password
        /// </summary>
        /// <param name="request">Change password request</param>
        /// <response code="200">Password changed successfully. Possible messages:  
        /// - PASSWORD_CHANGED_SUCCESSFULLY  
        /// </response>
        /// <response code="400">Validation error. Possible messages:  
        /// - CURRENT_PASSWORD_REQUIRED  
        /// - NEW_PASSWORD_REQUIRED  
        /// - CONFIRM_PASSWORD_REQUIRED  
        /// - NEW_PASSWORD_MIN_8_CHARACTERS  
        /// - NEW_PASSWORD_MUST_CONTAIN_UPPERCASE_LOWERCASE_NUMBER_SPECIAL  
        /// - CONFIRM_PASSWORD_MUST_MATCH  
        /// - CURRENT_PASSWORD_INCORRECT  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="404">User not found. Possible messages:  
        /// - USER_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Changes user password with proper validation and security measures.  
        /// 
        /// **Security Requirements:**  
        /// - Current password must be provided and verified  
        /// - New password must be at least 8 characters  
        /// - New password must contain uppercase, lowercase, number, and special character  
        /// - New password confirmation must match  
        /// 
        /// **Password Policy:**  
        /// - Minimum 8 characters  
        /// - At least 1 uppercase letter (A-Z)  
        /// - At least 1 lowercase letter (a-z)  
        /// - At least 1 number (0-9)  
        /// - At least 1 special character (@$!%*?&)  
        /// </remarks>
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _userProfileService.ChangePasswordAsync(userId, request);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets user's vehicles summary (owned, co-owned, invitations)
        /// </summary>
        /// <response code="200">Vehicles summary retrieved successfully. Possible messages:  
        /// - USER_VEHICLES_SUMMARY_RETRIEVED_SUCCESSFULLY  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Retrieves comprehensive summary of user's vehicle relationships including:  
        /// - **Owned Vehicles:** Vehicles created by the user (primary owner)  
        /// - **Co-owned Vehicles:** Vehicles where user has accepted co-ownership  
        /// - **Pending Invitations:** Outstanding invitations to become co-owner  
        /// 
        /// Each vehicle entry includes ownership percentage, investment amount, and status.
        /// </remarks>
        [HttpGet("vehicles")]
        public async Task<IActionResult> GetMyVehiclesSummary()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _userProfileService.GetUserVehiclesSummaryAsync(userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets user's activity summary (recent bookings, payments, license info)
        /// </summary>
        /// <response code="200">Activity summary retrieved successfully. Possible messages:  
        /// - USER_ACTIVITY_SUMMARY_RETRIEVED_SUCCESSFULLY  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Retrieves user's recent activity and important account information including:  
        /// - **Recent Bookings:** Last 5 vehicle bookings with status and amounts  
        /// - **Recent Payments:** Last 5 payments with methods and status  
        /// - **Driving License:** Current license status, expiry, and validity  
        /// 
        /// This provides a dashboard-style overview of user activity for the profile page.
        /// </remarks>
        [HttpGet("activity")]
        public async Task<IActionResult> GetMyActivitySummary()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _userProfileService.GetUserActivitySummaryAsync(userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Uploads user profile image
        /// </summary>
        /// <param name="imageFile">Profile image file</param>
        /// <response code="200">Profile image uploaded successfully. Possible messages:  
        /// - PROFILE_IMAGE_UPLOADED_SUCCESSFULLY  
        /// </response>
        /// <response code="400">Validation error. Possible messages:  
        /// - IMAGE_FILE_REQUIRED  
        /// - INVALID_IMAGE_FORMAT  
        /// - IMAGE_SIZE_TOO_LARGE  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="404">User not found. Possible messages:  
        /// - USER_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// - IMAGE_UPLOAD_FAILED  
        /// </response>
        /// <remarks>
        /// Uploads a new profile image for the authenticated user.  
        /// 
        /// **File Requirements:**  
        /// - **Supported formats:** JPEG, PNG, JPG, WEBP  
        /// - **Maximum size:** 5MB  
        /// - **Recommended dimensions:** 400x400 pixels (square)  
        /// 
        /// **Process:**  
        /// 1. Validates file format and size  
        /// 2. Uploads to file storage system  
        /// 3. Updates user profile with new image URL  
        /// 4. Optionally deletes old profile image  
        /// 
        /// Returns updated profile information including new image URL.
        /// </remarks>
        [HttpPost("upload-image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadProfileImage(IFormFile imageFile)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _userProfileService.UploadProfileImageAsync(userId, imageFile);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Deletes user profile image
        /// </summary>
        /// <response code="200">Profile image deleted successfully. Possible messages:  
        /// - PROFILE_IMAGE_DELETED_SUCCESSFULLY  
        /// </response>
        /// <response code="400">No profile image to delete. Possible messages:  
        /// - NO_PROFILE_IMAGE_TO_DELETE  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="404">User not found. Possible messages:  
        /// - USER_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Removes the current profile image for the authenticated user.  
        /// Sets the profile image URL to null and optionally deletes the file from storage.
        /// </remarks>
        [HttpDelete("delete-image")]
        public async Task<IActionResult> DeleteProfileImage()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _userProfileService.DeleteProfileImageAsync(userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Validates profile completeness for eligibility checks
        /// </summary>
        /// <response code="200">Profile validation completed. Possible messages:  
        /// - PROFILE_COMPLETE  
        /// - PROFILE_INCOMPLETE  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="404">User not found. Possible messages:  
        /// - USER_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Validates whether user profile is complete enough for system features.  
        /// 
        /// **Checked Fields:**  
        /// - First Name (required)  
        /// - Last Name (required)  
        /// - Phone Number (required)  
        /// - Date of Birth (required)  
        /// - Address (required)  
        /// - Profile Image (optional)  
        /// 
        /// **Response includes:**  
        /// - Completion status (true/false)  
        /// - Completion percentage (0-100%)  
        /// - List of missing required fields  
        /// 
        /// Used for Co-owner eligibility and other feature access requirements.
        /// </remarks>
        [HttpGet("validate-completeness")]
        public async Task<IActionResult> ValidateProfileCompleteness()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _userProfileService.ValidateProfileCompletenessAsync(userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        #region Development/Testing Endpoints

        /// <summary>
        /// Test endpoint for profile data scenarios (Development only)
        /// </summary>
        /// <response code="200">Test scenarios generated</response>
        /// <remarks>
        /// Provides mock profile scenarios for testing and development purposes.
        /// </remarks>
        [HttpGet("test/profile-scenarios")]
        public IActionResult TestProfileScenarios()
        {
            var scenarios = new
            {
                CompleteProfile = new
                {
                    Description = "User with complete profile information",
                    Data = new
                    {
                        FirstName = "Nguyen",
                        LastName = "Van A",
                        Phone = "+84912345678",
                        DateOfBirth = "1990-01-01",
                        Address = "123 Nguyen Hue, District 1, Ho Chi Minh City",
                        ProfileImageUrl = "https://example.com/profile.jpg",
                        CompletionPercentage = 100
                    }
                },
                IncompleteProfile = new
                {
                    Description = "User with missing profile information",
                    Data = new
                    {
                        FirstName = "Tran",
                        LastName = "Thi B",
                        Phone = "",
                        DateOfBirth = (DateOnly?)null,
                        Address = "",
                        ProfileImageUrl = (string?)null,
                        CompletionPercentage = 33,
                        MissingFields = new[] { "Phone", "DateOfBirth", "Address" }
                    }
                },
                StatsExample = new
                {
                    Description = "Example of user profile statistics",
                    Data = new
                    {
                        TotalVehiclesOwned = 2,
                        TotalVehiclesCoOwned = 3,
                        TotalBookings = 15,
                        PendingInvitations = 1,
                        HasValidDrivingLicense = true,
                        TotalPayments = 12,
                        TotalInvestmentAmount = 2500000000
                    }
                }
            };

            return Ok(new
            {
                StatusCode = 200,
                Message = "PROFILE_SCENARIOS_GENERATED",
                Data = scenarios
            });
        }

        #endregion
    }
}