using EvCoOwnership.API.Attributes;
using EvCoOwnership.DTOs.AuthDTOs;
using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Controller for driving license verification and management
    /// </summary>
    [Route("api/shared/license")]
    [ApiController]
    public class LicenseController : ControllerBase
    {
        private readonly ILicenseVerificationService _licenseVerificationService;
        private readonly ILogger<LicenseController> _logger;

        /// <summary>
        /// Initializes a new instance of the LicenseController
        /// </summary>
        /// <param name="licenseVerificationService">License verification service</param>
        /// <param name="logger">Logger</param>
        public LicenseController(ILicenseVerificationService licenseVerificationService, ILogger<LicenseController> logger)
        {
            _licenseVerificationService = licenseVerificationService;
            _logger = logger;
        }

        /// <summary>
        /// Verifies a driving license with the provided information
        /// </summary>
        /// <param name="request">License verification request containing license details</param>
        /// <response code="200">License verification successful. Possible messages:  
        /// - LICENSE_VERIFICATION_SUCCESS  
        /// </response>
        /// <response code="400">License verification failed. Possible messages:  
        /// - LICENSE_VERIFICATION_FAILED  
        /// - LICENSE_NUMBER_REQUIRED  
        /// - LICENSE_NUMBER_INVALID_LENGTH  
        /// - LICENSE_NUMBER_INVALID_FORMAT  
        /// - ISSUE_DATE_REQUIRED  
        /// - ISSUE_DATE_CANNOT_BE_FUTURE  
        /// - ISSUED_BY_REQUIRED  
        /// - ISSUED_BY_MAX_100_CHARACTERS  
        /// - ISSUED_BY_INVALID_FORMAT  
        /// - FIRST_NAME_REQUIRED  
        /// - FIRST_NAME_MAX_50_CHARACTERS  
        /// - FIRST_NAME_ONLY_LETTERS_AND_SPACES  
        /// - LAST_NAME_REQUIRED  
        /// - LAST_NAME_MAX_50_CHARACTERS  
        /// - LAST_NAME_ONLY_LETTERS_AND_SPACES  
        /// - DATE_OF_BIRTH_REQUIRED  
        /// - MUST_BE_AT_LEAST_18_YEARS_OLD  
        /// - DATE_OF_BIRTH_TOO_OLD  
        /// - INVALID_IMAGE_FILE  
        /// - IMAGE_SIZE_TOO_LARGE  
        /// </response>
        /// <response code="409">License already registered. Possible messages:  
        /// - LICENSE_ALREADY_REGISTERED  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Verifies a driving license against government databases and internal records.  
        /// Supports Vietnamese driving license formats and validates all provided information.
        /// </remarks>
        [HttpPost("verify")]
        public async Task<IActionResult> VerifyLicense([FromForm] VerifyLicenseRequest request)
        {
            var response = await _licenseVerificationService.VerifyLicenseAsync(request);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                409 => Conflict(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Checks if a license number is already registered in the system
        /// </summary>
        /// <param name="licenseNumber">License number to check</param>
        /// <response code="200">Check completed successfully. Possible messages:  
        /// - LICENSE_EXISTS  
        /// - LICENSE_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Quickly checks if a license number is already registered without performing full verification.  
        /// Useful for preventing duplicate registrations.
        /// </remarks>
        [HttpGet("check-exists")]
        public async Task<IActionResult> CheckLicenseExists([FromQuery] string licenseNumber)
        {
            if (string.IsNullOrEmpty(licenseNumber))
            {
                return BadRequest(new { Message = "LICENSE_NUMBER_REQUIRED" });
            }

            var response = await _licenseVerificationService.CheckLicenseExistsAsync(licenseNumber);
            return response.StatusCode switch
            {
                200 => Ok(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets license information by license number (requires authentication)
        /// </summary>
        /// <param name="licenseNumber">License number to lookup</param>
        /// <response code="200">License information retrieved successfully. Possible messages:  
        /// - SUCCESS  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied. Possible messages:  
        /// - ACCESS_DENIED  
        /// </response>
        /// <response code="404">Not found. Possible messages:  
        /// - USER_NOT_FOUND  
        /// - LICENSE_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Retrieves detailed license information. Users can only view their own licenses,  
        /// while administrators can view any license.
        /// </remarks>
        [HttpGet("info")]
        [AuthorizeRoles]
        public async Task<IActionResult> GetLicenseInfo([FromQuery] string licenseNumber)
        {
            if (string.IsNullOrEmpty(licenseNumber))
            {
                return BadRequest(new { Message = "LICENSE_NUMBER_REQUIRED" });
            }

            // Get user ID from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _licenseVerificationService.GetLicenseInfoAsync(licenseNumber, userId);
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
        /// Updates license status (admin only)
        /// </summary>
        /// <param name="licenseNumber">License number to update</param>
        /// <param name="status">New status for the license</param>
        /// <response code="200">License status updated successfully. Possible messages:  
        /// - LICENSE_STATUS_UPDATED  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied. Possible messages:  
        /// - ACCESS_DENIED  
        /// </response>
        /// <response code="404">Not found. Possible messages:  
        /// - USER_NOT_FOUND  
        /// - LICENSE_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Updates the status of a license. Only administrators and staff members can perform this action.
        /// </remarks>
        [HttpPatch("status")]
        [AuthorizeRoles(EUserRole.Admin, EUserRole.Staff)]
        public async Task<IActionResult> UpdateLicenseStatus([FromQuery] string licenseNumber, [FromQuery] string status)
        {
            if (string.IsNullOrEmpty(licenseNumber))
            {
                return BadRequest(new { Message = "LICENSE_NUMBER_REQUIRED" });
            }

            if (string.IsNullOrEmpty(status))
            {
                return BadRequest(new { Message = "STATUS_REQUIRED" });
            }

            // Get user ID from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var adminUserId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _licenseVerificationService.UpdateLicenseStatusAsync(licenseNumber, status, adminUserId);
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
        /// Gets license information for a specific user (requires authentication)
        /// </summary>
        /// <param name="userId">User ID to get license for</param>
        /// <response code="200">License information retrieved successfully. Possible messages:  
        /// - SUCCESS  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied. Possible messages:  
        /// - ACCESS_DENIED  
        /// </response>
        /// <response code="404">Not found. Possible messages:  
        /// - USER_NOT_FOUND  
        /// - LICENSE_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Retrieves license information for a specific user. Users can only view their own licenses,  
        /// while administrators can view any user's license.
        /// </remarks>
        [HttpGet("user/{userId:int}")]
        [AuthorizeRoles]
        public async Task<IActionResult> GetUserLicense(int userId)
        {
            // Get current user ID from JWT token
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentUserIdClaim, out var currentUserId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            // Check if user is trying to access their own data or is admin/staff
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("Staff");
            if (!isAdmin && currentUserId != userId)
            {
                return Forbid("ACCESS_DENIED");
            }

            var response = await _licenseVerificationService.GetUserLicenseAsync(userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Updates license information (user can update their own, admin/staff can update any)
        /// </summary>
        /// <param name="licenseId">License ID to update</param>
        /// <param name="request">Updated license information</param>
        /// <response code="200">License updated successfully. Possible messages:  
        /// - LICENSE_UPDATED_SUCCESSFULLY  
        /// </response>
        /// <response code="400">Bad request. Possible validation messages:  
        /// - LICENSE_NUMBER_REQUIRED  
        /// - LICENSE_NUMBER_INVALID_FORMAT  
        /// - ISSUE_DATE_REQUIRED  
        /// - ISSUED_BY_REQUIRED  
        /// - FIRST_NAME_REQUIRED  
        /// - LAST_NAME_REQUIRED  
        /// - DATE_OF_BIRTH_REQUIRED  
        /// - MUST_BE_AT_LEAST_18_YEARS_OLD  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied. Possible messages:  
        /// - ACCESS_DENIED  
        /// </response>
        /// <response code="404">Not found. Possible messages:  
        /// - LICENSE_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Updates existing license information. Users can only update their own licenses,  
        /// while administrators and staff can update any license.
        /// </remarks>
        [HttpPut("{licenseId:int}")]
        [AuthorizeRoles]
        public async Task<IActionResult> UpdateLicense(int licenseId, [FromForm] VerifyLicenseRequest request)
        {
            // Get current user ID from JWT token
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentUserIdClaim, out var currentUserId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _licenseVerificationService.UpdateLicenseAsync(licenseId, request, currentUserId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => Forbid(response.Message),
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Deletes a license (admin/staff only or user can delete their own)
        /// </summary>
        /// <param name="licenseId">License ID to delete</param>
        /// <response code="200">License deleted successfully. Possible messages:  
        /// - LICENSE_DELETED_SUCCESSFULLY  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied. Possible messages:  
        /// - ACCESS_DENIED  
        /// </response>
        /// <response code="404">Not found. Possible messages:  
        /// - LICENSE_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Deletes a license from the system. Users can only delete their own licenses,  
        /// while administrators and staff can delete any license.
        /// </remarks>
        [HttpDelete("{licenseId:int}")]
        [AuthorizeRoles]
        public async Task<IActionResult> DeleteLicense(int licenseId)
        {
            // Get current user ID from JWT token
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentUserIdClaim, out var currentUserId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _licenseVerificationService.DeleteLicenseAsync(licenseId, currentUserId);
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
        /// Registers a verified license to the system (requires authentication)
        /// </summary>
        /// <param name="request">License registration request</param>
        /// <response code="201">License registered successfully. Possible messages:  
        /// - LICENSE_REGISTERED_SUCCESSFULLY  
        /// </response>
        /// <response code="400">License verification failed. Possible messages:  
        /// - LICENSE_VERIFICATION_FAILED  
        /// - LICENSE_NUMBER_REQUIRED  
        /// - ISSUE_DATE_REQUIRED  
        /// - ISSUED_BY_REQUIRED  
        /// - FIRST_NAME_REQUIRED  
        /// - LAST_NAME_REQUIRED  
        /// - DATE_OF_BIRTH_REQUIRED  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="409">License already registered. Possible messages:  
        /// - LICENSE_ALREADY_REGISTERED  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Verifies and registers a driving license to the authenticated user's account.  
        /// The license must pass verification before it can be registered.
        /// </remarks>
        [HttpPost("register")]
        [AuthorizeRoles]
        public async Task<IActionResult> RegisterLicense([FromForm] VerifyLicenseRequest request)
        {
            try
            {
                // Log all incoming request data for debugging
                _logger.LogInformation("[RegisterLicense] Incoming request: LicenseNumber={LicenseNumber}, IssueDate={IssueDate}, IssuedBy={IssuedBy}, FirstName={FirstName}, LastName={LastName}, DateOfBirth={DateOfBirth}, LicenseImage={LicenseImageName}, LicenseImageSize={LicenseImageSize}",
                    request.LicenseNumber,
                    request.IssueDate,
                    request.IssuedBy,
                    request.FirstName,
                    request.LastName,
                    request.DateOfBirth,
                    request.LicenseImage?.FileName,
                    request.LicenseImage?.Length);

                // Get current user ID from JWT token
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(currentUserIdClaim, out var currentUserId))
                {
                    _logger.LogWarning("[RegisterLicense] INVALID_TOKEN: Cannot parse user ID from JWT claim. Raw claim: {Claim}", currentUserIdClaim);
                    return Unauthorized(new { Message = "INVALID_TOKEN" });
                }

                _logger.LogInformation("[RegisterLicense] Registering license for user ID: {UserId}", currentUserId);
                var response = await _licenseVerificationService.RegisterLicenseAsync(request, currentUserId);
                _logger.LogInformation("[RegisterLicense] License registration response: StatusCode={StatusCode}, Message={Message}, Data={Data}, Errors={Errors}",
                    response.StatusCode, response.Message, response.Data, response.Errors);

                return response.StatusCode switch
                {
                    201 => Created($"/api/license/user/{currentUserId}", response),
                    400 => BadRequest(response),
                    409 => Conflict(response),
                    500 => StatusCode(500, response),
                    _ => StatusCode(response.StatusCode, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RegisterLicense] Error occurred during license registration");
                return StatusCode(500, new BaseResponse { StatusCode = 500, Message = "Internal Server Error" });
            }
        }

        /// <summary>
        /// Gets the authenticated user's license information
        /// </summary>
        /// <response code="200">License information retrieved successfully. Possible messages:  
        /// - SUCCESS  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="404">Not found. Possible messages:  
        /// - LICENSE_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Retrieves the driving license information for the currently authenticated user.
        /// </remarks>
        [HttpGet("my-license")]
        [AuthorizeRoles]
        public async Task<IActionResult> GetMyLicense()
        {
            // Get current user ID from JWT token
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentUserIdClaim, out var currentUserId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _licenseVerificationService.GetUserLicenseAsync(currentUserId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }
    }
}