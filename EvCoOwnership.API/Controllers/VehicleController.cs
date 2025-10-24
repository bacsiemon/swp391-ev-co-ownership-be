using EvCoOwnership.API.Attributes;
using EvCoOwnership.DTOs.VehicleDTOs;
using EvCoOwnership.Repositories.Enums;
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
        [AuthorizeRoles(EUserRole.CoOwner)]
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
        [AuthorizeRoles(EUserRole.CoOwner, EUserRole.Staff, EUserRole.Admin)]
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
        [AuthorizeRoles(EUserRole.CoOwner)]
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
        [HttpGet("{vehicleId:int}/details")]
        [AuthorizeRoles(EUserRole.CoOwner)]
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
        /// **MY VEHICLES - Private View**
        /// 
        /// Retrieves only vehicles where the current user is an owner or co-owner.  
        /// Includes both active and pending co-ownership relationships.
        /// 
        /// **Difference from /api/vehicle/available:**
        /// - `/my-vehicles` → Only shows vehicles YOU own (private)
        /// - `/available` → Shows ALL vehicles in marketplace (public discovery)
        /// 
        /// **Use Cases:**
        /// - View my investment portfolio
        /// - Check my vehicle ownership status
        /// - Manage my co-owned vehicles
        /// </remarks>
        [HttpGet("my-vehicles")]
        [AuthorizeRoles(EUserRole.CoOwner)]
        public async Task<IActionResult> GetMyVehicles()
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
        [AuthorizeRoles(EUserRole.CoOwner)]
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
        [AuthorizeRoles(EUserRole.CoOwner, EUserRole.Staff, EUserRole.Admin)]
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
        [AuthorizeRoles(EUserRole.CoOwner, EUserRole.Staff, EUserRole.Admin)]
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

        /// <summary>
        /// Gets all available vehicles for co-ownership or booking with pagination and filters
        /// </summary>
        /// <param name="pageIndex">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 10, max: 50)</param>
        /// <param name="status">Filter by vehicle status (Available, InUse, Maintenance, Unavailable)</param>
        /// <param name="verificationStatus">Filter by verification status (Pending, Verified, Rejected, etc.)</param>
        /// <param name="brand">Filter by brand (partial match, case-insensitive). Example: "VinFast", "Tesla", "BMW"</param>
        /// <param name="model">Filter by model (partial match, case-insensitive). Example: "VF8", "Model 3", "i4"</param>
        /// <param name="minYear">Minimum manufacturing year. Example: 2020</param>
        /// <param name="maxYear">Maximum manufacturing year. Example: 2024</param>
        /// <param name="minPrice">Minimum purchase price in VND. Example: 500000000 (500 million VND)</param>
        /// <param name="maxPrice">Maximum purchase price in VND. Example: 2000000000 (2 billion VND)</param>
        /// <param name="search">Search keyword across multiple fields (name, brand, model, VIN, license plate). Case-insensitive.</param>
        /// <param name="sortBy">Sort field: name, brand, model, year, price, createdAt. Default: createdAt</param>
        /// <param name="sortDesc">Sort direction: true for descending (default), false for ascending</param>
        /// <response code="200">Available vehicles retrieved successfully. Possible messages:  
        /// - AVAILABLE_VEHICLES_RETRIEVED_SUCCESSFULLY  
        /// </response>
        /// <response code="400">Bad request. Possible messages:  
        /// - INVALID_PAGE_INDEX  
        /// - INVALID_PAGE_SIZE  
        /// - INVALID_STATUS_FILTER  
        /// - INVALID_VERIFICATION_STATUS_FILTER  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// **AVAILABLE VEHICLES - Role-Based Access with Comprehensive Filtering**
        /// 
        /// **Access Control by Role:**
        /// - **Co-owner**: Can only see vehicles in their co-ownership groups (vehicles they are part of)
        /// - **Staff/Admin**: Can see ALL vehicles in the system
        /// 
        /// This ensures privacy - co-owners only discover vehicles within their existing groups,
        /// while staff/admin have full visibility for management purposes.
        /// 
        /// **IMPORTANT - Business Model:**
        /// - Co-owners: Limited to their group's vehicles (private view)
        /// - Staff/Admin: Full platform access (public view)
        /// - Security: Only verified vehicles shown by default
        /// 
        /// **Difference from /api/vehicle/my-vehicles:**
        /// - `/available` → Vehicles you CAN access based on role (group vehicles for co-owner, all for staff/admin)
        /// - `/my-vehicles` → Only vehicles YOU own (private portfolio)
        /// 
        /// **Default Behavior (no filters):**  
        /// - Status: Available only  
        /// - VerificationStatus: Verified only  
        /// - Sorted by: Newest first (CreatedAt descending)  
        /// 
        /// **Available Status Filters:**  
        /// - Available: Vehicle is ready for booking  
        /// - InUse: Currently being used (bookings in progress)  
        /// - Maintenance: Under maintenance or repair  
        /// - Unavailable: Not available for any reason  
        /// 
        /// **Available Verification Status Filters:**  
        /// - Pending: Awaiting verification  
        /// - VerificationRequested: Verification process started  
        /// - RequiresRecheck: Needs re-verification  
        /// - Verified: Fully verified and approved  
        /// - Rejected: Verification rejected  
        /// 
        /// **Additional Filtering Options:**
        /// - **Brand Filter**: Partial text match, case-insensitive (e.g., "vin" matches "VinFast")
        /// - **Model Filter**: Partial text match, case-insensitive (e.g., "vf" matches "VF8", "VF9")
        /// - **Year Range**: Filter vehicles manufactured between minYear and maxYear (inclusive)
        /// - **Price Range**: Filter by purchase price in VND (minPrice to maxPrice)
        /// - **Keyword Search**: Search across name, brand, model, VIN, and license plate simultaneously
        /// 
        /// **Sorting Options:**
        /// - **name**: Vehicle name (alphabetical)
        /// - **brand**: Brand name (alphabetical)
        /// - **model**: Model name (alphabetical)
        /// - **year**: Manufacturing year (chronological)
        /// - **price**: Purchase price (numerical)
        /// - **createdAt**: Date added to system (default, chronological)
        /// - Direction: sortDesc=true (descending, default) or false (ascending)
        /// 
        /// **Response Includes:**  
        /// - Vehicle details (brand, model, year, specs)  
        /// - Current co-owners with ownership percentages  
        /// - Available ownership percentage (100% - total active ownership)  
        /// - Location information (latitude, longitude)  
        /// - Status and verification information  
        /// 
        /// **Use Cases:**  
        /// - Co-owners: Browse vehicles in their groups for booking
        /// - Co-owners: Find investment opportunities within their groups
        /// - Staff/Admin: View all vehicles for management and oversight
        /// - Filter by brand/model to find specific vehicle types
        /// - Search by VIN or license plate for vehicle lookup
        /// - Filter by price range to match budget constraints
        /// - Sort by year to find newest/oldest vehicles
        /// - Check vehicle specifications before booking or investment  
        /// 
        /// **Pagination:**  
        /// - Default: 10 items per page  
        /// - Maximum: 50 items per page  
        /// - Returns total count for pagination UI  
        /// 
        /// **Privacy and Security:**
        /// - Role-based filtering protects vehicle privacy
        /// - Co-owners can only see their group's vehicles
        /// - Only verified vehicles shown by default (safety)
        /// - Investment amounts visible within groups for transparency
        /// 
        /// **Example Requests:**  
        /// 
        /// **1. Basic (default filters):**  
        /// GET /api/vehicle/available
        /// 
        /// **2. Filter by brand:**  
        /// GET /api/vehicle/available?brand=VinFast
        /// 
        /// **3. Filter by model:**  
        /// GET /api/vehicle/available?model=VF8
        /// 
        /// **4. Price range (500M - 2B VND):**  
        /// GET /api/vehicle/available?minPrice=500000000&amp;maxPrice=2000000000
        /// 
        /// **5. Year range (2020-2024):**  
        /// GET /api/vehicle/available?minYear=2020&amp;maxYear=2024
        /// 
        /// **6. Combined brand + year + status:**  
        /// GET /api/vehicle/available?brand=Tesla&amp;minYear=2022&amp;status=Available
        /// 
        /// **7. Search by keyword:**  
        /// GET /api/vehicle/available?search=VF8
        /// 
        /// **8. Sort by price (ascending):**  
        /// GET /api/vehicle/available?sortBy=price&amp;sortDesc=false
        /// 
        /// **9. Sort by year (newest first):**  
        /// GET /api/vehicle/available?sortBy=year&amp;sortDesc=true
        /// 
        /// **10. Complex filter (brand + price range + sort):**  
        /// GET /api/vehicle/available?brand=VinFast&amp;minPrice=1000000000&amp;maxPrice=3000000000&amp;sortBy=price&amp;sortDesc=false&amp;pageSize=20
        /// 
        /// **11. Search + year range:**  
        /// GET /api/vehicle/available?search=electric&amp;minYear=2023&amp;sortBy=createdAt
        /// 
        /// **12. Admin view all with status filter:**  
        /// GET /api/vehicle/available?status=Maintenance&amp;verificationStatus=Verified
        /// </remarks>
        [HttpGet("available")]
        [AuthorizeRoles(EUserRole.CoOwner, EUserRole.Staff, EUserRole.Admin)]
        public async Task<IActionResult> GetAvailableVehicles(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            [FromQuery] string? verificationStatus = null,
            [FromQuery] string? brand = null,
            [FromQuery] string? model = null,
            [FromQuery] int? minYear = null,
            [FromQuery] int? maxYear = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDesc = true)
        {
            // Validate pagination parameters
            if (pageIndex < 1)
            {
                return BadRequest(new { Message = "INVALID_PAGE_INDEX", Details = "Page index must be at least 1" });
            }

            if (pageSize < 1 || pageSize > 50)
            {
                return BadRequest(new { Message = "INVALID_PAGE_SIZE", Details = "Page size must be between 1 and 50" });
            }

            // Get user ID from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _vehicleService.GetAvailableVehiclesAsync(
                userId, pageIndex, pageSize, status, verificationStatus,
                brand, model, minYear, maxYear, minPrice, maxPrice, search, sortBy, sortDesc);
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
        /// Gets detailed vehicle information including fund, co-owners, and creator
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <response code="200">Vehicle detail retrieved successfully. Possible messages:  
        /// - VEHICLE_DETAIL_RETRIEVED_SUCCESSFULLY  
        /// </response>
        /// <response code="403">Access denied. Possible messages:  
        /// - ACCESS_DENIED_NOT_VEHICLE_CO_OWNER  
        /// - ACCESS_DENIED_INSUFFICIENT_PERMISSIONS  
        /// - CO_OWNER_PROFILE_NOT_FOUND  
        /// </response>
        /// <response code="404">Not found. Possible messages:  
        /// - VEHICLE_NOT_FOUND  
        /// - USER_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// **VEHICLE DETAIL - Role-Based Access**
        /// 
        /// **Access Control:**
        /// - **Co-owner**: Can only view details of vehicles they are part of (Active co-owner status required)
        /// - **Staff/Admin**: Can view details of any vehicle
        /// 
        /// **Response Includes:**
        /// - Complete vehicle specifications (brand, model, year, VIN, license plate, color, battery, range)
        /// - Purchase and financial information (purchase date, price, warranty)
        /// - Current status (distance travelled, status, verification status)
        /// - Location information (latitude, longitude)
        /// - Complete co-ownership information:
        ///   - List of all co-owners with contact details
        ///   - Individual ownership percentages and investment amounts
        ///   - Co-owner status (Active, Pending, Rejected, Inactive)
        ///   - Total ownership percentage
        ///   - Available ownership percentage (100% - total active ownership)
        /// - Fund information (if exists):
        ///   - Current fund balance
        ///   - Total number of additions and usages
        ///   - Total added and used amounts
        ///   - Fund creation and update timestamps
        /// - Creator information (user who created the vehicle)
        /// 
        /// **Use Cases:**
        /// - Co-owners: View complete details of their vehicles
        /// - Staff/Admin: Manage and verify vehicle information
        /// - Check fund balance before planning expenses
        /// - Review co-ownership distribution
        /// - Verify vehicle specifications and status
        /// 
        /// **Example Request:**  
        /// GET /api/vehicle/123
        /// </remarks>
        [HttpGet("{vehicleId}")]
        [AuthorizeRoles(EUserRole.CoOwner, EUserRole.Staff, EUserRole.Admin)]
        public async Task<IActionResult> GetVehicleDetail(int vehicleId)
        {
            // Get user ID from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _vehicleService.GetVehicleDetailAsync(vehicleId, userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
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

        #region Vehicle Availability Endpoints

        /// <summary>
        /// Gets vehicle availability schedule showing booked and free time slots
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="startDate">Start date of the period (format: yyyy-MM-dd)</param>
        /// <param name="endDate">End date of the period (format: yyyy-MM-dd)</param>
        /// <param name="statusFilter">Optional: Filter bookings by status (Confirmed, Pending, etc.)</param>
        /// <response code="200">Vehicle availability schedule retrieved successfully</response>
        /// <response code="400">Invalid date range or parameters</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - not a co-owner of this vehicle</response>
        /// <response code="404">Vehicle or user not found</response>
        /// <remarks>
        /// **VEHICLE AVAILABILITY SCHEDULE - View When a Vehicle is Free/Busy**
        /// 
        /// **Purpose:**
        /// This endpoint provides a detailed view of when a specific vehicle is available or booked,
        /// helping co-owners plan their usage and avoid conflicts.
        /// 
        /// **Access Control:**
        /// - **Co-owner**: Can only view schedule of vehicles they co-own
        /// - **Staff/Admin**: Can view schedule of any vehicle
        /// 
        /// **Response Includes:**
        /// 1. **Vehicle Information**: Name, brand, model, license plate, current status
        /// 2. **Booked Time Slots**: All bookings in the period with:
        ///    - Who booked it (co-owner name)
        ///    - Start and end time
        ///    - Purpose of booking
        ///    - Booking status
        /// 3. **Available Days**: List of days with NO bookings at all
        /// 4. **Utilization Statistics**:
        ///    - Total booked hours vs available hours
        ///    - Utilization percentage (how busy the vehicle is)
        ///    - Number of bookings (total, confirmed, pending)
        ///    - Average booking duration
        /// 
        /// **Use Cases:**
        /// - **Plan Your Booking**: See when the vehicle is free before creating a booking
        /// - **Coordination**: See who else is using the vehicle and when
        /// - **Usage Analysis**: Understand how much the vehicle is being utilized
        /// - **Find Patterns**: Identify peak usage times
        /// 
        /// **Date Range:**
        /// - Maximum: 90 days
        /// - Recommended: 7-30 days for typical planning
        /// 
        /// **Example Requests:**
        /// 
        /// **1. View next week's schedule:**
        /// GET /api/vehicle/5/availability/schedule?startDate=2025-01-17&amp;endDate=2025-01-24
        /// 
        /// **2. View next month (confirmed bookings only):**
        /// GET /api/vehicle/5/availability/schedule?startDate=2025-01-17&amp;endDate=2025-02-17&amp;statusFilter=Confirmed
        /// 
        /// **3. Check January availability:**
        /// GET /api/vehicle/5/availability/schedule?startDate=2025-01-01&amp;endDate=2025-01-31
        /// </remarks>
        [HttpGet("{vehicleId}/availability/schedule")]
        [AuthorizeRoles(EUserRole.CoOwner, EUserRole.Staff, EUserRole.Admin)]
        public async Task<IActionResult> GetVehicleAvailabilitySchedule(
            int vehicleId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] string? statusFilter = null)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _vehicleService.GetVehicleAvailabilityScheduleAsync(
                vehicleId, userId, startDate, endDate, statusFilter);

            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Finds available time slots for booking a specific vehicle
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="startDate">Start date to search (format: yyyy-MM-dd)</param>
        /// <param name="endDate">End date to search (format: yyyy-MM-dd)</param>
        /// <param name="minimumDurationHours">Minimum duration required in hours (default: 1, max: 24)</param>
        /// <param name="fullDayOnly">Only return full-day slots (8+ hours) (default: false)</param>
        /// <response code="200">Available time slots found</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied</response>
        /// <response code="404">Vehicle not found</response>
        /// <remarks>
        /// **FIND AVAILABLE TIME SLOTS - Smart Booking Suggestions**
        /// 
        /// **Purpose:**
        /// Automatically finds time slots when the vehicle is available for booking,
        /// saving you from manually checking the calendar.
        /// 
        /// **How It Works:**
        /// 1. Analyzes all confirmed bookings in the period
        /// 2. Identifies gaps between bookings
        /// 3. Filters slots by your minimum duration requirement
        /// 4. Returns sorted list of available slots
        /// 
        /// **Parameters:**
        /// - `minimumDurationHours`: Minimum hours needed (e.g., 4 hours for a short trip, 8+ for full day)
        /// - `fullDayOnly`: Set to true to only see full-day (8+ hour) slots
        /// 
        /// **Use Cases:**
        /// 
        /// **1. Quick Trip Planning:**
        /// - "I need the car for 3 hours next week"
        /// - Set minimumDurationHours=3, get all 3+ hour slots
        /// 
        /// **2. Full Day Booking:**
        /// - "I need the car for a full day trip"
        /// - Set fullDayOnly=true, only see 8+ hour slots
        /// 
        /// **3. Weekend Planning:**
        /// - "When can I book the car this weekend?"
        /// - Check Saturday-Sunday with minimum duration
        /// 
        /// **4. Flexible Scheduling:**
        /// - "Show me any available time in the next 2 weeks"
        /// - Wide date range, low minimum duration
        /// 
        /// **Response Includes:**
        /// - List of available slots with:
        ///   - Start and end time
        ///   - Duration in hours
        ///   - Whether it's a full day
        ///   - Recommendation (e.g., "Full day available", "4 hours between bookings")
        /// - Total number of slots found
        /// - Helpful message if no slots match criteria
        /// 
        /// **Example Requests:**
        /// 
        /// **1. Find any 2+ hour slots next week:**
        /// GET /api/vehicle/5/availability/find-slots?startDate=2025-01-17&amp;endDate=2025-01-24&amp;minimumDurationHours=2
        /// 
        /// **2. Find full-day slots in January:**
        /// GET /api/vehicle/5/availability/find-slots?startDate=2025-01-01&amp;endDate=2025-01-31&amp;fullDayOnly=true
        /// 
        /// **3. Find 6+ hour slots for weekend:**
        /// GET /api/vehicle/5/availability/find-slots?startDate=2025-01-20&amp;endDate=2025-01-22&amp;minimumDurationHours=6
        /// </remarks>
        [HttpGet("{vehicleId}/availability/find-slots")]
        [AuthorizeRoles(EUserRole.CoOwner, EUserRole.Staff, EUserRole.Admin)]
        public async Task<IActionResult> FindAvailableTimeSlots(
            int vehicleId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] int minimumDurationHours = 1,
            [FromQuery] bool fullDayOnly = false)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _vehicleService.FindAvailableTimeSlotsAsync(
                vehicleId, userId, startDate, endDate, minimumDurationHours, fullDayOnly);

            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Compares utilization of multiple vehicles in user's group
        /// </summary>
        /// <param name="startDate">Start date of comparison period (format: yyyy-MM-dd)</param>
        /// <param name="endDate">End date of comparison period (format: yyyy-MM-dd)</param>
        /// <response code="200">Vehicle utilization comparison retrieved successfully</response>
        /// <response code="400">Invalid date range</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied</response>
        /// <response code="404">No vehicles found</response>
        /// <remarks>
        /// **VEHICLE UTILIZATION COMPARISON - Which Vehicle is Used Most?**
        /// 
        /// **Purpose:**
        /// Compare how frequently different vehicles are being used,
        /// helping identify which vehicles are popular and which are underutilized.
        /// 
        /// **Access Control:**
        /// - **Co-owner**: Compares only vehicles in their co-ownership groups
        /// - **Staff/Admin**: Compares all vehicles in the system
        /// 
        /// **Metrics Compared:**
        /// 1. **Utilization Percentage**: % of time the vehicle is booked (booked hours / total hours)
        /// 2. **Total Bookings**: Number of bookings in the period
        /// 3. **Total Booked Hours**: How many hours the vehicle was reserved
        /// 4. **Most Active Day**: Which day of the week the vehicle is booked most
        /// 
        /// **Response Includes:**
        /// - List of all vehicles with their utilization stats
        /// - Most utilized vehicle (highest utilization %)
        /// - Least utilized vehicle (lowest utilization %)
        /// - Average utilization across all vehicles
        /// 
        /// **Use Cases:**
        /// 
        /// **1. Fleet Management (Admin/Staff):**
        /// - Identify underutilized vehicles (consider selling?)
        /// - Identify over-utilized vehicles (need more of this type?)
        /// - Balance fleet composition
        /// 
        /// **2. Co-owner Group Insights:**
        /// - See which vehicle is most popular in your group
        /// - Decide which vehicle to book (choose less busy one)
        /// - Understand group usage patterns
        /// 
        /// **3. Investment Decisions:**
        /// - High utilization = good ROI, consider investing more
        /// - Low utilization = might want to reduce ownership stake
        /// 
        /// **4. Booking Strategy:**
        /// - Choose the least utilized vehicle for easier booking
        /// - Avoid peak-demand vehicles if you're flexible
        /// 
        /// **Example Requests:**
        /// 
        /// **1. Compare last month:**
        /// GET /api/vehicle/utilization/compare?startDate=2025-01-01&amp;endDate=2025-01-31
        /// 
        /// **2. Compare last quarter:**
        /// GET /api/vehicle/utilization/compare?startDate=2024-10-01&amp;endDate=2024-12-31
        /// 
        /// **3. Compare last 30 days:**
        /// GET /api/vehicle/utilization/compare?startDate=2024-12-18&amp;endDate=2025-01-17
        /// 
        /// **Example Response Insights:**
        /// ```
        /// - VinFast VF8: 65% utilization (most popular)
        /// - Tesla Model 3: 45% utilization
        /// - BMW i4: 20% utilization (underutilized)
        /// - Average: 43.3% utilization
        /// 
        /// Recommendation: VF8 is in high demand, consider booking early.
        /// BMW i4 has lots of availability, easier to book anytime.
        /// ```
        /// </remarks>
        [HttpGet("utilization/compare")]
        [AuthorizeRoles(EUserRole.CoOwner, EUserRole.Staff, EUserRole.Admin)]
        public async Task<IActionResult> CompareVehicleUtilization(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _vehicleService.CompareVehicleUtilizationAsync(userId, startDate, endDate);

            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        #endregion
    }
}