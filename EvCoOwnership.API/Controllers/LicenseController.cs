using EvCoOwnership.DTOs.AuthDTOs;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Controller for driving license verification and management
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class LicenseController : ControllerBase
    {
        private readonly ILicenseVerificationService _licenseVerificationService;

        /// <summary>
        /// Initializes a new instance of the LicenseController
        /// </summary>
        /// <param name="licenseVerificationService">License verification service</param>
        public LicenseController(ILicenseVerificationService licenseVerificationService)
        {
            _licenseVerificationService = licenseVerificationService;
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
        [Authorize]
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
        [Authorize(Roles = "Admin,Staff")]
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

        #region Development/Testing Endpoints

        /// <summary>
        /// Test license verification with mock data (Development only)
        /// </summary>
        /// <response code="200">Test successful</response>
        /// <remarks>
        /// This endpoint is for testing purposes only and provides mock verification scenarios.
        /// </remarks>
        [HttpGet("test/mock-verification")]
        public IActionResult TestMockVerification()
        {
            var mockResults = new
            {
                ValidLicense = new
                {
                    LicenseNumber = "123456789",
                    Status = "VERIFIED",
                    Message = "Mock verification successful"
                },
                InvalidLicense = new
                {
                    LicenseNumber = "INVALID123",
                    Status = "INVALID_FORMAT",
                    Message = "Mock verification failed - invalid format"
                },
                ExpiredLicense = new
                {
                    LicenseNumber = "987654321",
                    Status = "EXPIRED",
                    Message = "Mock verification failed - license expired"
                }
            };

            return Ok(new
            {
                StatusCode = 200,
                Message = "MOCK_DATA_GENERATED",
                Data = mockResults
            });
        }

        /// <summary>
        /// Test endpoint for license format validation (Development only)
        /// </summary>
        /// <param name="licenseNumber">License number to test</param>
        /// <response code="200">Test completed</response>
        /// <remarks>
        /// Tests various license number formats for validation rules.
        /// </remarks>
        [HttpGet("test/validate-format")]
        public IActionResult TestValidateFormat([FromQuery] string licenseNumber)
        {
            if (string.IsNullOrEmpty(licenseNumber))
            {
                return BadRequest(new { Message = "LICENSE_NUMBER_REQUIRED" });
            }

            var validationResults = new
            {
                LicenseNumber = licenseNumber,
                IsValidVietnameseFormat = Services.Mapping.LicenseMapper.IsValidVietnameseLicenseFormat(licenseNumber),
                Length = licenseNumber.Length,
                HasLetters = licenseNumber.Any(char.IsLetter),
                HasNumbers = licenseNumber.Any(char.IsDigit),
                FirstCharacter = licenseNumber.FirstOrDefault(),
                TestPatterns = new
                {
                    NineDigits = System.Text.RegularExpressions.Regex.IsMatch(licenseNumber, @"^[0-9]{9}$"),
                    LetterPlusEightDigits = System.Text.RegularExpressions.Regex.IsMatch(licenseNumber, @"^[A-Z][0-9]{8}$"),
                    TwelveDigits = System.Text.RegularExpressions.Regex.IsMatch(licenseNumber, @"^[0-9]{12}$")
                }
            };

            return Ok(new
            {
                StatusCode = 200,
                Message = "VALIDATION_TEST_COMPLETED",
                Data = validationResults
            });
        }

        #endregion
    }
}