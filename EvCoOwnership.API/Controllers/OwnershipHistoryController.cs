using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.OwnershipDTOs;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Controller for managing ownership history and tracking
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OwnershipHistoryController : ControllerBase
    {
        private readonly IOwnershipChangeService _ownershipChangeService;
        private readonly ILogger<OwnershipHistoryController> _logger;

        public OwnershipHistoryController(
            IOwnershipChangeService ownershipChangeService,
            ILogger<OwnershipHistoryController> logger)
        {
            _ownershipChangeService = ownershipChangeService;
            _logger = logger;
        }

        /// <summary>
        /// **[CoOwner]** Get ownership history for a vehicle with filters
        /// </summary>
        /// <remarks>
        /// **Parameters:**
        /// - `vehicleId` (path): The ID of the vehicle
        /// - `changeType` (query, optional): Filter by change type (Initial/Adjustment/Transfer/Exit/NewMember/AdminAdjustment)
        /// - `startDate` (query, optional): Filter by start date (ISO 8601 format)
        /// - `endDate` (query, optional): Filter by end date (ISO 8601 format)
        /// - `coOwnerId` (query, optional): Filter by specific co-owner
        /// - `offset` (query, optional): Pagination offset (default: 0)
        /// - `limit` (query, optional): Pagination limit (default: 50)
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/ownershiphistory/vehicle/1?changeType=Adjustment&amp;limit=20
        /// Authorization: Bearer {token}
        /// ```
        /// 
        /// **Sample Response (200 OK):**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "FOUND_3_OWNERSHIP_HISTORY_RECORDS",
        ///   "data": [
        ///     {
        ///       "id": 1,
        ///       "vehicleId": 1,
        ///       "vehicleName": "Tesla Model 3",
        ///       "licensePlate": "30A-12345",
        ///       "coOwnerId": 1,
        ///       "userId": 5,
        ///       "coOwnerName": "John Doe",
        ///       "email": "john@example.com",
        ///       "ownershipChangeRequestId": 1,
        ///       "previousPercentage": 40.00,
        ///       "newPercentage": 45.00,
        ///       "percentageChange": 5.00,
        ///       "previousInvestment": 400000000.00,
        ///       "newInvestment": 450000000.00,
        ///       "investmentChange": 50000000.00,
        ///       "changeType": "Adjustment",
        ///       "reason": "Increased investment contribution",
        ///       "changedByUserId": 5,
        ///       "changedByName": "John Doe",
        ///       "createdAt": "2024-01-15T10:30:00Z"
        ///     }
        ///   ],
        ///   "additionalData": {
        ///     "totalCount": 3,
        ///     "offset": 0,
        ///     "limit": 50
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Ownership history retrieved successfully</response>
        /// <response code="403">NOT_AUTHORIZED_TO_VIEW_VEHICLE_HISTORY - User is not a co-owner of this vehicle</response>
        /// <response code="500">INTERNAL_SERVER_ERROR - Server error occurred</response>
        [HttpGet("vehicle/{vehicleId}")]
        [ProducesResponseType(typeof(BaseResponse<List<OwnershipHistoryResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<OwnershipHistoryResponse>>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetVehicleOwnershipHistory(
            int vehicleId,
            [FromQuery] string? changeType = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? coOwnerId = null,
            [FromQuery] int offset = 0,
            [FromQuery] int limit = 50)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var request = new GetOwnershipHistoryRequest
            {
                ChangeType = changeType,
                StartDate = startDate,
                EndDate = endDate,
                CoOwnerId = coOwnerId,
                Offset = offset,
                Limit = limit
            };

            var response = await _ownershipChangeService.GetVehicleOwnershipHistoryAsync(vehicleId, userId, request);

            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// **[CoOwner]** Get complete ownership timeline for a vehicle
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Returns the complete ownership evolution timeline for a vehicle, showing all co-owners and their ownership changes over time.
        /// 
        /// **Parameters:**
        /// - `vehicleId` (path): The ID of the vehicle
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/ownershiphistory/vehicle/1/timeline
        /// Authorization: Bearer {token}
        /// ```
        /// 
        /// **Sample Response (200 OK):**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "VEHICLE_OWNERSHIP_TIMELINE_RETRIEVED_SUCCESSFULLY",
        ///   "data": {
        ///     "vehicleId": 1,
        ///     "vehicleName": "Tesla Model 3",
        ///     "licensePlate": "30A-12345",
        ///     "vehicleCreatedAt": "2023-06-01T08:00:00Z",
        ///     "totalHistoryRecords": 5,
        ///     "coOwnersTimeline": [
        ///       {
        ///         "coOwnerId": 1,
        ///         "userId": 5,
        ///         "coOwnerName": "John Doe",
        ///         "email": "john@example.com",
        ///         "currentPercentage": 45.00,
        ///         "initialPercentage": 40.00,
        ///         "totalChange": 5.00,
        ///         "joinedAt": "2023-06-01T08:00:00Z",
        ///         "changeCount": 2,
        ///         "changes": [...]
        ///       }
        ///     ],
        ///     "allChanges": [...]
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Ownership timeline retrieved successfully</response>
        /// <response code="403">NOT_AUTHORIZED_TO_VIEW_VEHICLE_TIMELINE - User is not a co-owner of this vehicle</response>
        /// <response code="404">VEHICLE_NOT_FOUND - Vehicle does not exist</response>
        /// <response code="500">INTERNAL_SERVER_ERROR - Server error occurred</response>
        [HttpGet("vehicle/{vehicleId}/timeline")]
        [ProducesResponseType(typeof(BaseResponse<VehicleOwnershipTimelineResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<VehicleOwnershipTimelineResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<VehicleOwnershipTimelineResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetVehicleOwnershipTimeline(int vehicleId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var response = await _ownershipChangeService.GetVehicleOwnershipTimelineAsync(vehicleId, userId);

            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// **[CoOwner]** Get ownership snapshot at a specific date
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Returns the ownership distribution as it was at a specific date in the past.
        /// 
        /// **Parameters:**
        /// - `vehicleId` (path): The ID of the vehicle
        /// - `date` (query): The date for the snapshot (ISO 8601 format, e.g., 2024-01-15T00:00:00Z)
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/ownershiphistory/vehicle/1/snapshot?date=2024-01-15T00:00:00Z
        /// Authorization: Bearer {token}
        /// ```
        /// 
        /// **Sample Response (200 OK):**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "OWNERSHIP_SNAPSHOT_RETRIEVED_SUCCESSFULLY",
        ///   "data": {
        ///     "vehicleId": 1,
        ///     "vehicleName": "Tesla Model 3",
        ///     "licensePlate": "30A-12345",
        ///     "snapshotDate": "2024-01-15T00:00:00Z",
        ///     "totalPercentage": 100.00,
        ///     "coOwners": [
        ///       {
        ///         "coOwnerId": 1,
        ///         "userId": 5,
        ///         "coOwnerName": "John Doe",
        ///         "email": "john@example.com",
        ///         "ownershipPercentage": 40.00,
        ///         "investmentAmount": 400000000.00
        ///       },
        ///       {
        ///         "coOwnerId": 2,
        ///         "userId": 6,
        ///         "coOwnerName": "Jane Smith",
        ///         "email": "jane@example.com",
        ///         "ownershipPercentage": 60.00,
        ///         "investmentAmount": 600000000.00
        ///       }
        ///     ]
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Ownership snapshot retrieved successfully</response>
        /// <response code="403">NOT_AUTHORIZED_TO_VIEW_VEHICLE_SNAPSHOT - User is not a co-owner of this vehicle</response>
        /// <response code="404">VEHICLE_NOT_FOUND - Vehicle does not exist</response>
        /// <response code="500">INTERNAL_SERVER_ERROR - Server error occurred</response>
        [HttpGet("vehicle/{vehicleId}/snapshot")]
        [ProducesResponseType(typeof(BaseResponse<OwnershipSnapshotResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<OwnershipSnapshotResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<OwnershipSnapshotResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOwnershipSnapshot(
            int vehicleId,
            [FromQuery] DateTime date)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var response = await _ownershipChangeService.GetOwnershipSnapshotAsync(vehicleId, date, userId);

            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// **[CoOwner]** Get ownership history statistics for a vehicle
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Returns aggregated statistics about ownership changes for a vehicle.
        /// 
        /// **Parameters:**
        /// - `vehicleId` (path): The ID of the vehicle
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/ownershiphistory/vehicle/1/statistics
        /// Authorization: Bearer {token}
        /// ```
        /// 
        /// **Sample Response (200 OK):**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "OWNERSHIP_HISTORY_STATISTICS_RETRIEVED_SUCCESSFULLY",
        ///   "data": {
        ///     "vehicleId": 1,
        ///     "vehicleName": "Tesla Model 3",
        ///     "totalChanges": 5,
        ///     "totalCoOwners": 3,
        ///     "currentCoOwners": 2,
        ///     "firstChange": "2023-07-01T10:00:00Z",
        ///     "lastChange": "2024-01-15T14:30:00Z",
        ///     "averageOwnershipPercentage": 50.00,
        ///     "mostActiveCoOwnerId": 1,
        ///     "mostActiveCoOwnerName": "John Doe",
        ///     "mostActiveCoOwnerChanges": 3,
        ///     "changeTypeBreakdown": {
        ///       "Adjustment": 3,
        ///       "Transfer": 1,
        ///       "Exit": 1
        ///     },
        ///     "statisticsGeneratedAt": "2024-01-16T10:00:00Z"
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Ownership statistics retrieved successfully</response>
        /// <response code="403">NOT_AUTHORIZED_TO_VIEW_VEHICLE_STATISTICS - User is not a co-owner of this vehicle</response>
        /// <response code="404">VEHICLE_NOT_FOUND - Vehicle does not exist</response>
        /// <response code="500">INTERNAL_SERVER_ERROR - Server error occurred</response>
        [HttpGet("vehicle/{vehicleId}/statistics")]
        [ProducesResponseType(typeof(BaseResponse<OwnershipHistoryStatisticsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<OwnershipHistoryStatisticsResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<OwnershipHistoryStatisticsResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOwnershipHistoryStatistics(int vehicleId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var response = await _ownershipChangeService.GetOwnershipHistoryStatisticsAsync(vehicleId, userId);

            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// **[CoOwner]** Get ownership history for the current co-owner across all vehicles
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Returns the complete ownership history for the authenticated co-owner across all vehicles they are or were involved with.
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/ownershiphistory/my-history
        /// Authorization: Bearer {token}
        /// ```
        /// 
        /// **Sample Response (200 OK):**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "FOUND_8_OWNERSHIP_HISTORY_RECORDS",
        ///   "data": [
        ///     {
        ///       "id": 1,
        ///       "vehicleId": 1,
        ///       "vehicleName": "Tesla Model 3",
        ///       "licensePlate": "30A-12345",
        ///       "coOwnerId": 1,
        ///       "userId": 5,
        ///       "coOwnerName": "John Doe",
        ///       "email": "john@example.com",
        ///       "ownershipChangeRequestId": 1,
        ///       "previousPercentage": 40.00,
        ///       "newPercentage": 45.00,
        ///       "percentageChange": 5.00,
        ///       "previousInvestment": 400000000.00,
        ///       "newInvestment": 450000000.00,
        ///       "investmentChange": 50000000.00,
        ///       "changeType": "Adjustment",
        ///       "reason": "Increased investment contribution",
        ///       "changedByUserId": 5,
        ///       "changedByName": "John Doe",
        ///       "createdAt": "2024-01-15T10:30:00Z"
        ///     }
        ///   ]
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Co-owner ownership history retrieved successfully</response>
        /// <response code="404">CO_OWNER_NOT_FOUND - User is not a co-owner</response>
        /// <response code="500">INTERNAL_SERVER_ERROR - Server error occurred</response>
        [HttpGet("my-history")]
        [ProducesResponseType(typeof(BaseResponse<List<OwnershipHistoryResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<OwnershipHistoryResponse>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyOwnershipHistory()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var response = await _ownershipChangeService.GetCoOwnerOwnershipHistoryAsync(userId);

            return response.StatusCode switch
            {
                200 => Ok(response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }
    }
}
