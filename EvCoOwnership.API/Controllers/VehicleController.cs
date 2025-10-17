using EvCoOwnership.DTOs.VehicleDTOs;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Controller for vehicle management operations
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "CoOwner")]
    public class VehicleController : ControllerBase
    {
        private readonly IVehicleService _vehicleService;
        private readonly ILogger<VehicleController> _logger;

        /// <summary>
        /// Initializes a new instance of the VehicleController
        /// </summary>
        public VehicleController(IVehicleService vehicleService, ILogger<VehicleController> logger)
        {
            _vehicleService = vehicleService;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new vehicle
        /// </summary>
        /// <param name="request">Vehicle creation request</param>
        /// <response code="201">Vehicle created successfully. Possible messages:  
        /// - VEHICLE_CREATED_SUCCESSFULLY  
        /// </response>
        /// <response code="400">Validation error or user not eligible. Possible messages:  
        /// - VEHICLE_NAME_REQUIRED  
        /// - BRAND_REQUIRED  
        /// - MODEL_REQUIRED  
        /// - VIN_REQUIRED  
        /// - VIN_INVALID_FORMAT  
        /// - LICENSE_PLATE_REQUIRED  
        /// - LICENSE_PLATE_INVALID_FORMAT  
        /// - PURCHASE_DATE_REQUIRED  
        /// - PURCHASE_PRICE_REQUIRED  
        /// - USER_NOT_ELIGIBLE_TO_CREATE_VEHICLE  
        /// - USER_ACCOUNT_NOT_ACTIVE  
        /// - NO_DRIVING_LICENSE_REGISTERED  
        /// - DRIVING_LICENSE_NOT_VERIFIED  
        /// - DRIVING_LICENSE_EXPIRED  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="409">Vehicle already exists. Possible messages:  
        /// - LICENSE_PLATE_ALREADY_EXISTS  
        /// - VIN_ALREADY_EXISTS  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Creates a new vehicle in the EV co-ownership system. The user creating the vehicle becomes the primary owner.  
        /// 
        /// **Requirements:**  
        /// - User must have Co-owner role  
        /// - User must have verified driving license  
        /// - License must not be expired  
        /// - VIN and license plate must be unique  
        /// 
        /// **Vietnamese License Plate Format:**  
        /// - Old format: 30A-123.45 (2 digits + 1-2 letters + dash + 3 digits + dot + 2 digits)  
        /// - New format: 30A-12345 (2 digits + 1-2 letters + dash + 4-5 digits)  
        /// 
        /// **VIN Format:**  
        /// - Must be exactly 17 characters  
        /// - Only letters A-H, J-N, P-R, Z and numbers 0-9 allowed  
        /// - No I, O, Q allowed to avoid confusion  
        /// </remarks>
        [HttpPost]
        public async Task<IActionResult> CreateVehicle([FromBody] CreateVehicleRequest request)
        {
            // Get user ID from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _vehicleService.CreateVehicleAsync(request, userId);
            return response.StatusCode switch
            {
                201 => StatusCode(201, response),
                400 => BadRequest(response),
                409 => Conflict(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Adds a co-owner to an existing vehicle
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="request">Add co-owner request</param>
        /// <response code="200">Co-owner invitation sent successfully. Possible messages:  
        /// - CO_OWNER_INVITATION_SENT_SUCCESSFULLY  
        /// </response>
        /// <response code="400">Validation error. Possible messages:  
        /// - USER_ID_REQUIRED  
        /// - OWNERSHIP_PERCENTAGE_REQUIRED  
        /// - INVESTMENT_AMOUNT_REQUIRED  
        /// - TARGET_USER_NOT_CO_OWNER  
        /// - OWNERSHIP_PERCENTAGE_EXCEEDS_LIMIT  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied. Possible messages:  
        /// - ACCESS_DENIED_NOT_CO_OWNER  
        /// - ACCESS_DENIED_NOT_VEHICLE_CO_OWNER  
        /// </response>
        /// <response code="404">Not found. Possible messages:  
        /// - VEHICLE_NOT_FOUND  
        /// - TARGET_USER_NOT_FOUND  
        /// </response>
        /// <response code="409">Conflict. Possible messages:  
        /// - USER_ALREADY_CO_OWNER_OF_VEHICLE  
        /// - INVITATION_ALREADY_PENDING  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Adds a co-owner to an existing vehicle by sending an invitation.  
        /// 
        /// **Requirements:**  
        /// - Requesting user must be a co-owner of the vehicle  
        /// - Target user must have Co-owner role  
        /// - Target user must not already be a co-owner of this vehicle  
        /// - Ownership percentage must not exceed available percentage (total cannot exceed 100%)  
        /// 
        /// **Business Rules:**  
        /// - Investment amount should be proportional to ownership percentage  
        /// - Invitation is created in pending status  
        /// - Target user must accept invitation to become active co-owner  
        /// 
        /// **Example:**  
        /// If vehicle has 70% ownership taken, maximum new ownership percentage is 30%
        /// </remarks>
        [HttpPost("{vehicleId:int}/co-owners")]
        public async Task<IActionResult> AddCoOwner(int vehicleId, [FromBody] AddCoOwnerRequest request)
        {
            // Get user ID from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _vehicleService.AddCoOwnerAsync(vehicleId, request, userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                409 => Conflict(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Responds to a co-ownership invitation (accept or reject)
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="request">Response to invitation request</param>
        /// <response code="200">Invitation response processed successfully. Possible messages:  
        /// - INVITATION_ACCEPTED_SUCCESSFULLY  
        /// - INVITATION_REJECTED_SUCCESSFULLY  
        /// </response>
        /// <response code="400">Validation error. Possible messages:  
        /// - RESPONSE_REQUIRED  
        /// - USER_NOT_CO_OWNER  
        /// - OWNERSHIP_PERCENTAGE_NO_LONGER_VALID  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="404">Not found. Possible messages:  
        /// - INVITATION_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Allows a user to accept or reject a co-ownership invitation.  
        /// 
        /// **Requirements:**  
        /// - User must have pending invitation for the vehicle  
        /// - Ownership percentage must still be valid (not exceeded by other acceptances)  
        /// 
        /// **Business Rules:**  
        /// - When accepting: Status changes from Pending to Active  
        /// - When rejecting: Status changes from Pending to Rejected  
        /// - System re-validates ownership percentage before accepting  
        /// - Investment commitment becomes binding upon acceptance  
        /// 
        /// **Legal Compliance (Vietnam):**  
        /// - Co-ownership agreement becomes legally binding upon acceptance  
        /// - All parties have equal rights to vehicle usage based on ownership percentage  
        /// - Investment amount must be documented for tax purposes  
        /// </remarks>
        [HttpPut("{vehicleId:int}/invitations/respond")]
        public async Task<IActionResult> RespondToInvitation(int vehicleId, [FromBody] RespondToInvitationRequest request)
        {
            // Get user ID from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _vehicleService.RespondToInvitationAsync(vehicleId, request, userId);
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
        /// Gets vehicle information including co-owners
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <response code="200">Vehicle information retrieved successfully. Possible messages:  
        /// - VEHICLE_RETRIEVED_SUCCESSFULLY  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied. Possible messages:  
        /// - ACCESS_DENIED  
        /// </response>
        /// <response code="404">Not found. Possible messages:  
        /// - VEHICLE_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Retrieves detailed vehicle information including all co-owners.  
        /// Only co-owners of the vehicle can access this information.
        /// </remarks>
        [HttpGet("{vehicleId:int}")]
        public async Task<IActionResult> GetVehicle(int vehicleId)
        {
            // Get user ID from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _vehicleService.GetVehicleAsync(vehicleId, userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets all vehicles for the current user (as owner or co-owner)
        /// </summary>
        /// <response code="200">User vehicles retrieved successfully. Possible messages:  
        /// - USER_VEHICLES_RETRIEVED_SUCCESSFULLY  
        /// </response>
        /// <response code="400">User not co-owner. Possible messages:  
        /// - USER_NOT_CO_OWNER  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Retrieves all vehicles where the current user is an owner or co-owner.  
        /// Includes both active and pending co-ownership relationships.
        /// </remarks>
        [HttpGet("my-vehicles")]
        public async Task<IActionResult> GetUserVehicles()
        {
            // Get user ID from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _vehicleService.GetUserVehiclesAsync(userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets pending co-ownership invitations for the current user
        /// </summary>
        /// <response code="200">Pending invitations retrieved successfully. Possible messages:  
        /// - PENDING_INVITATIONS_RETRIEVED_SUCCESSFULLY  
        /// </response>
        /// <response code="400">User not co-owner. Possible messages:  
        /// - USER_NOT_CO_OWNER  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Retrieves all pending co-ownership invitations for the current user.  
        /// These are invitations that haven't been accepted or rejected yet.
        /// </remarks>
        [HttpGet("invitations/pending")]
        public async Task<IActionResult> GetPendingInvitations()
        {
            // Get user ID from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _vehicleService.GetPendingInvitationsAsync(userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Removes a co-owner from a vehicle (vehicle creator only)
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="coOwnerUserId">ID of the co-owner to remove</param>
        /// <response code="200">Co-owner removed successfully. Possible messages:  
        /// - CO_OWNER_REMOVED_SUCCESSFULLY  
        /// </response>
        /// <response code="400">Validation error. Possible messages:  
        /// - CANNOT_REMOVE_LAST_ACTIVE_OWNER  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied. Possible messages:  
        /// - ACCESS_DENIED_ONLY_CREATOR_CAN_REMOVE  
        /// </response>
        /// <response code="404">Not found. Possible messages:  
        /// - VEHICLE_NOT_FOUND  
        /// - TARGET_USER_NOT_CO_OWNER  
        /// - CO_OWNER_RELATIONSHIP_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Removes a co-owner from a vehicle. Only the vehicle creator can perform this action.  
        /// Cannot remove the last active owner of the vehicle.
        /// </remarks>
        [HttpDelete("{vehicleId:int}/co-owners/{coOwnerUserId:int}")]
        public async Task<IActionResult> RemoveCoOwner(int vehicleId, int coOwnerUserId)
        {
            // Get user ID from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _vehicleService.RemoveCoOwnerAsync(vehicleId, coOwnerUserId, userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Updates vehicle information (co-owners only)
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="request">Vehicle update request</param>
        /// <response code="200">Vehicle updated successfully. Possible messages:  
        /// - VEHICLE_UPDATED_SUCCESSFULLY  
        /// </response>
        /// <response code="400">Validation error</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied. Possible messages:  
        /// - ACCESS_DENIED_NOT_CO_OWNER  
        /// - ACCESS_DENIED_NOT_VEHICLE_CO_OWNER  
        /// </response>
        /// <response code="404">Not found. Possible messages:  
        /// - VEHICLE_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Updates vehicle information. Only active co-owners can update vehicle details.  
        /// Note: VIN and license plate cannot be changed as they are permanent identifiers.
        /// </remarks>
        [HttpPut("{vehicleId:int}")]
        public async Task<IActionResult> UpdateVehicle(int vehicleId, [FromBody] CreateVehicleRequest request)
        {
            // Get user ID from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _vehicleService.UpdateVehicleAsync(vehicleId, request, userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        #region Development/Testing Endpoints

        /// <summary>
        /// Validates if the current user can create a vehicle (Development only)
        /// </summary>
        [HttpGet("validate-creation-eligibility")]
        public async Task<IActionResult> ValidateCreationEligibility()
        {
            // Get user ID from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _vehicleService.ValidateVehicleCreationEligibilityAsync(userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        #endregion
    }
}