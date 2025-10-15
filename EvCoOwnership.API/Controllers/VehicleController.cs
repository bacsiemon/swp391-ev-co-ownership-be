using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EvCoOwnership.Services.Interfaces;
using EvCoOwnership.Repositories.DTOs.VehicleDTOs;
using EvCoOwnership.Repositories.Enums;
using System.Security.Claims;

namespace EvCoOwnership.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehicleController : ControllerBase
    {
        private readonly IVehicleVerificationService _vehicleVerificationService;

        public VehicleController(IVehicleVerificationService vehicleVerificationService)
        {
            _vehicleVerificationService = vehicleVerificationService;
        }

        /// <summary>
        /// Creates a new vehicle (Admin/Staff only)
        /// </summary>
        /// <param name="request">Vehicle creation data</param>
        /// <response code="201">Vehicle created successfully. Possible messages:
        /// - VEHICLE_CREATED_SUCCESSFULLY
        /// </response>
        /// <response code="400">Bad request. Possible messages:
        /// - VIN_ALREADY_EXISTS
        /// - LICENSE_PLATE_ALREADY_EXISTS
        /// - VEHICLE_NAME_REQUIRED
        /// - VEHICLE_BRAND_REQUIRED
        /// - VEHICLE_MODEL_REQUIRED
        /// - INVALID_VEHICLE_YEAR
        /// - VIN_REQUIRED
        /// - INVALID_VIN_FORMAT
        /// - LICENSE_PLATE_REQUIRED
        /// - INVALID_VIETNAM_LICENSE_PLATE_FORMAT
        /// - PURCHASE_PRICE_MUST_BE_POSITIVE
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - Admin/Staff only</response>
        /// <response code="500">Internal server error</response>
        /// <remarks>
        /// Creates a new vehicle in the system. The vehicle will be set to pending verification status.
        /// 
        /// **Vehicle Information Requirements:**
        /// - Name: Vehicle display name (max 200 characters)
        /// - Brand and Model: Required for identification
        /// - Year: Must be between 1900 and current year + 1
        /// - VIN: Must be exactly 17 characters, alphanumeric (excluding I, O, Q)
        /// - License Plate: Must follow Vietnamese format (e.g., "30A-123.45" or "51B-1234")
        /// - Purchase Price: Must be positive
        /// 
        /// **Vietnamese License Plate Formats:**
        /// - Format 1: ##X-###.## (e.g., "30A-123.45")
        /// - Format 2: ##XX-#### (e.g., "51B-1234")
        /// 
        /// **VIN Format:**
        /// - Exactly 17 characters
        /// - Letters A-H, J-N, P-R, S-Z and numbers 0-9
        /// - Excludes letters I, O, Q to avoid confusion
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> CreateVehicle([FromBody] VehicleCreateRequest request)
        {
            // Get user ID from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _vehicleVerificationService.CreateVehicleAsync(request, userId);
            return response.StatusCode switch
            {
                201 => Created("", response),
                400 => BadRequest(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets detailed vehicle information including verification history
        /// </summary>
        /// <param name="vehicleId">Vehicle ID</param>
        /// <response code="200">Vehicle information retrieved successfully</response>
        /// <response code="404">Vehicle not found</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{vehicleId}")]
        [Authorize]
        public async Task<IActionResult> GetVehicleDetail(int vehicleId)
        {
            var response = await _vehicleVerificationService.GetVehicleDetailAsync(vehicleId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets vehicles pending verification (Staff/Admin only)
        /// </summary>
        /// <response code="200">Pending vehicles retrieved successfully</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - Staff/Admin only</response>
        /// <response code="500">Internal server error</response>
        /// <remarks>
        /// Returns all vehicles with status:
        /// - Pending (newly created)
        /// - VerificationRequested (requested by owner)
        /// - RequiresRecheck (needs re-verification)
        /// </remarks>
        [HttpGet("pending-verification")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetVehiclesPendingVerification()
        {
            var response = await _vehicleVerificationService.GetVehiclesPendingVerificationAsync();
            return response.StatusCode switch
            {
                200 => Ok(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets vehicles by verification status (Staff/Admin only)
        /// </summary>
        /// <param name="status">Verification status (0=Pending, 1=VerificationRequested, 2=RequiresRecheck, 3=Verified, 4=Rejected)</param>
        /// <response code="200">Vehicles retrieved successfully</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - Staff/Admin only</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("by-status/{status}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetVehiclesByVerificationStatus(EVehicleVerificationStatus status)
        {
            var response = await _vehicleVerificationService.GetVehiclesByVerificationStatusAsync(status);
            return response.StatusCode switch
            {
                200 => Ok(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Verifies a vehicle (Staff/Admin only)
        /// </summary>
        /// <param name="request">Vehicle verification data</param>
        /// <response code="200">Vehicle verification completed successfully. Possible messages:
        /// - VEHICLE_VERIFICATION_COMPLETED
        /// </response>
        /// <response code="400">Bad request. Possible messages:
        /// - VEHICLE_ALREADY_VERIFIED
        /// - REJECTION_NOTES_REQUIRED
        /// - REJECTION_NOTES_MIN_10_CHARACTERS
        /// - CANNOT_SET_PENDING_STATUS
        /// - INVALID_VERIFICATION_STATUS
        /// </response>
        /// <response code="404">Vehicle not found</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - Staff/Admin only</response>
        /// <response code="500">Internal server error</response>
        /// <remarks>
        /// Performs vehicle verification by staff members.
        /// 
        /// **Verification Statuses:**
        /// - **VerificationRequested (1)**: Request submitted for verification
        /// - **RequiresRecheck (2)**: Requires additional verification
        /// - **Verified (3)**: Vehicle approved and ready for use
        /// - **Rejected (4)**: Vehicle rejected with mandatory notes
        /// 
        /// **Business Rules:**
        /// - Cannot set status to Pending (0) - only system can set this
        /// - Rejection requires detailed notes (minimum 10 characters)
        /// - Verified vehicles become available for booking
        /// - Rejected vehicles become unavailable
        /// - Image URLs must be valid if provided
        /// 
        /// **Verification Process:**
        /// 1. Staff reviews vehicle documents and physical condition
        /// 2. Verifies VIN matches registration
        /// 3. Checks license plate validity
        /// 4. Confirms vehicle safety and functionality
        /// 5. Sets appropriate status with notes
        /// </remarks>
        [HttpPost("verify")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> VerifyVehicle([FromBody] VehicleVerificationRequest request)
        {
            // Get user ID from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var staffId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _vehicleVerificationService.VerifyVehicleAsync(request, staffId);
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
        /// Gets verification history for a vehicle
        /// </summary>
        /// <param name="vehicleId">Vehicle ID</param>
        /// <response code="200">Verification history retrieved successfully</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{vehicleId}/verification-history")]
        [Authorize]
        public async Task<IActionResult> GetVerificationHistory(int vehicleId)
        {
            var response = await _vehicleVerificationService.GetVerificationHistoryAsync(vehicleId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Requests verification for a vehicle (Co-owner only)
        /// </summary>
        /// <param name="vehicleId">Vehicle ID to request verification for</param>
        /// <response code="200">Verification requested successfully. Possible messages:
        /// - VERIFICATION_REQUESTED_SUCCESSFULLY
        /// </response>
        /// <response code="400">Bad request. Possible messages:
        /// - VEHICLE_ALREADY_VERIFIED
        /// - VERIFICATION_ALREADY_REQUESTED
        /// </response>
        /// <response code="404">Vehicle not found</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - Co-owner only</response>
        /// <response code="500">Internal server error</response>
        /// <remarks>
        /// Allows vehicle owners to request verification from staff.
        /// Vehicle must not already be verified or have pending verification request.
        /// </remarks>
        [HttpPost("{vehicleId}/request-verification")]
        [Authorize(Roles = "CoOwner")]
        public async Task<IActionResult> RequestVerification(int vehicleId)
        {
            var response = await _vehicleVerificationService.RequestVerificationAsync(vehicleId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }
    }
}