using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.UsageAnalyticsDTOs;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Controller for usage analytics and usage vs ownership comparisons
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsageAnalyticsController : ControllerBase
    {
        private readonly IUsageAnalyticsService _usageAnalyticsService;
        private readonly ILogger<UsageAnalyticsController> _logger;

        public UsageAnalyticsController(
            IUsageAnalyticsService usageAnalyticsService,
            ILogger<UsageAnalyticsController> logger)
        {
            _usageAnalyticsService = usageAnalyticsService;
            _logger = logger;
        }

        /// <summary>
        /// **[CoOwner]** Get usage vs ownership comparison graph data for a vehicle
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Returns comprehensive comparison data between actual vehicle usage and ownership percentages for all co-owners.
        /// Shows who is using the vehicle more or less than their ownership share.
        /// 
        /// **Parameters:**
        /// - `vehicleId` (path): The ID of the vehicle
        /// - `startDate` (query, optional): Analysis start date (ISO 8601 format, defaults to vehicle creation)
        /// - `endDate` (query, optional): Analysis end date (ISO 8601 format, defaults to current date)
        /// - `usageMetric` (query, optional): Metric for usage calculation - "Hours" (default), "Distance", or "BookingCount"
        /// 
        /// **Usage Metrics:**
        /// - **Hours**: Total hours booked (EndTime - StartTime)
        /// - **Distance**: Total kilometers driven (from odometer readings in check-in/check-out)
        /// - **BookingCount**: Number of bookings made
        /// 
        /// **Usage Patterns:**
        /// - **Balanced**: Usage within ±5% of ownership percentage
        /// - **Overutilized**: Usage exceeds ownership by &gt;5%
        /// - **Underutilized**: Usage below ownership by &gt;5%
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/usageanalytics/vehicle/1/usage-vs-ownership?usageMetric=Hours
        /// Authorization: Bearer {token}
        /// ```
        /// 
        /// **Sample Response (200 OK):**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "USAGE_VS_OWNERSHIP_DATA_RETRIEVED_SUCCESSFULLY",
        ///   "data": {
        ///     "vehicleId": 1,
        ///     "vehicleName": "Tesla Model 3",
        ///     "licensePlate": "30A-12345",
        ///     "analysisStartDate": "2024-01-01T00:00:00Z",
        ///     "analysisEndDate": "2024-10-23T00:00:00Z",
        ///     "usageMetric": "Hours",
        ///     "coOwnersData": [
        ///       {
        ///         "coOwnerId": 1,
        ///         "userId": 5,
        ///         "coOwnerName": "John Doe",
        ///         "email": "john@example.com",
        ///         "ownershipPercentage": 40.00,
        ///         "investmentAmount": 400000000.00,
        ///         "usagePercentage": 55.50,
        ///         "actualUsageValue": 222.0,
        ///         "totalBookings": 15,
        ///         "completedBookings": 13,
        ///         "usageVsOwnershipDelta": 15.50,
        ///         "usagePattern": "Overutilized",
        ///         "fairUsageValue": 160.0
        ///       },
        ///       {
        ///         "coOwnerId": 2,
        ///         "userId": 6,
        ///         "coOwnerName": "Jane Smith",
        ///         "email": "jane@example.com",
        ///         "ownershipPercentage": 60.00,
        ///         "investmentAmount": 600000000.00,
        ///         "usagePercentage": 44.50,
        ///         "actualUsageValue": 178.0,
        ///         "totalBookings": 10,
        ///         "completedBookings": 9,
        ///         "usageVsOwnershipDelta": -15.50,
        ///         "usagePattern": "Underutilized",
        ///         "fairUsageValue": 240.0
        ///       }
        ///     ],
        ///     "summary": {
        ///       "totalUsageValue": 400.0,
        ///       "averageOwnershipPercentage": 50.00,
        ///       "averageUsagePercentage": 50.00,
        ///       "usageVariance": 15.50,
        ///       "totalBookings": 25,
        ///       "completedBookings": 22,
        ///       "mostActiveCoOwner": { "coOwnerId": 1, "coOwnerName": "John Doe", "usagePercentage": 55.50 },
        ///       "leastActiveCoOwner": { "coOwnerId": 2, "coOwnerName": "Jane Smith", "usagePercentage": 44.50 },
        ///       "balancedCoOwnersCount": 0,
        ///       "overutilizedCoOwnersCount": 1,
        ///       "underutilizedCoOwnersCount": 1
        ///     },
        ///     "generatedAt": "2024-10-23T10:30:00Z"
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Usage vs ownership data retrieved successfully</response>
        /// <response code="403">NOT_AUTHORIZED_TO_VIEW_VEHICLE_ANALYTICS - User is not a co-owner of this vehicle</response>
        /// <response code="404">VEHICLE_NOT_FOUND - Vehicle does not exist</response>
        /// <response code="500">INTERNAL_SERVER_ERROR - Server error occurred</response>
        [HttpGet("vehicle/{vehicleId}/usage-vs-ownership")]
        [ProducesResponseType(typeof(BaseResponse<UsageVsOwnershipResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<UsageVsOwnershipResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<UsageVsOwnershipResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUsageVsOwnership(
            int vehicleId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string usageMetric = "Hours")
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var request = new GetUsageVsOwnershipRequest
            {
                StartDate = startDate,
                EndDate = endDate,
                UsageMetric = usageMetric
            };

            var response = await _usageAnalyticsService.GetUsageVsOwnershipAsync(vehicleId, userId, request);

            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// **[CoOwner]** Get usage vs ownership trends over time
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Returns time-series data showing how usage patterns evolved compared to ownership over time.
        /// Useful for creating timeline/trend charts showing usage behavior changes.
        /// 
        /// **Parameters:**
        /// - `vehicleId` (path): The ID of the vehicle
        /// - `startDate` (query, optional): Analysis start date (defaults to vehicle creation)
        /// - `endDate` (query, optional): Analysis end date (defaults to current date)
        /// - `granularity` (query, optional): Time period granularity - "Daily", "Weekly", or "Monthly" (default)
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/usageanalytics/vehicle/1/usage-vs-ownership/trends?granularity=Monthly
        /// Authorization: Bearer {token}
        /// ```
        /// 
        /// **Sample Response (200 OK):**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "USAGE_VS_OWNERSHIP_TRENDS_RETRIEVED_SUCCESSFULLY",
        ///   "data": {
        ///     "vehicleId": 1,
        ///     "vehicleName": "Tesla Model 3",
        ///     "licensePlate": "30A-12345",
        ///     "analysisStartDate": "2024-01-01T00:00:00Z",
        ///     "analysisEndDate": "2024-10-23T00:00:00Z",
        ///     "granularity": "Monthly",
        ///     "trendData": [
        ///       {
        ///         "date": "2024-01-01T00:00:00Z",
        ///         "period": "Jan 2024",
        ///         "coOwnersData": [
        ///           {
        ///             "coOwnerId": 1,
        ///             "coOwnerName": "John Doe",
        ///             "ownershipPercentage": 40.00,
        ///             "usagePercentage": 60.00,
        ///             "usageValue": 48.0
        ///           },
        ///           {
        ///             "coOwnerId": 2,
        ///             "coOwnerName": "Jane Smith",
        ///             "ownershipPercentage": 60.00,
        ///             "usagePercentage": 40.00,
        ///             "usageValue": 32.0
        ///           }
        ///         ]
        ///       },
        ///       {
        ///         "date": "2024-02-01T00:00:00Z",
        ///         "period": "Feb 2024",
        ///         "coOwnersData": [...]
        ///       }
        ///     ],
        ///     "generatedAt": "2024-10-23T10:30:00Z"
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Usage vs ownership trends retrieved successfully</response>
        /// <response code="403">NOT_AUTHORIZED_TO_VIEW_VEHICLE_ANALYTICS - User is not a co-owner of this vehicle</response>
        /// <response code="404">VEHICLE_NOT_FOUND - Vehicle does not exist</response>
        /// <response code="500">INTERNAL_SERVER_ERROR - Server error occurred</response>
        [HttpGet("vehicle/{vehicleId}/usage-vs-ownership/trends")]
        [ProducesResponseType(typeof(BaseResponse<UsageVsOwnershipTrendsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<UsageVsOwnershipTrendsResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<UsageVsOwnershipTrendsResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUsageVsOwnershipTrends(
            int vehicleId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string granularity = "Monthly")
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var response = await _usageAnalyticsService.GetUsageVsOwnershipTrendsAsync(
                vehicleId, userId, startDate, endDate, granularity);

            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// **[CoOwner]** Get detailed usage breakdown for a specific co-owner
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Returns comprehensive usage details for a specific co-owner including all metrics,
        /// booking history, and usage patterns.
        /// 
        /// **Parameters:**
        /// - `vehicleId` (path): The ID of the vehicle
        /// - `coOwnerId` (path): The ID of the co-owner
        /// - `startDate` (query, optional): Analysis start date
        /// - `endDate` (query, optional): Analysis end date
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/usageanalytics/vehicle/1/co-owner/1/usage-detail
        /// Authorization: Bearer {token}
        /// ```
        /// 
        /// **Sample Response (200 OK):**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "CO_OWNER_USAGE_DETAIL_RETRIEVED_SUCCESSFULLY",
        ///   "data": {
        ///     "coOwnerId": 1,
        ///     "userId": 5,
        ///     "coOwnerName": "John Doe",
        ///     "email": "john@example.com",
        ///     "vehicleId": 1,
        ///     "vehicleName": "Tesla Model 3",
        ///     "ownershipPercentage": 40.00,
        ///     "usagePercentage": 55.50,
        ///     "usageVsOwnershipDelta": 15.50,
        ///     "usageMetrics": {
        ///       "totalHours": 222.0,
        ///       "hoursPercentage": 55.50,
        ///       "totalDistance": 1500.0,
        ///       "distancePercentage": 52.00,
        ///       "totalBookings": 15,
        ///       "bookingsPercentage": 60.00,
        ///       "completedBookings": 13,
        ///       "cancelledBookings": 2,
        ///       "averageBookingDuration": 14.80
        ///     },
        ///     "recentBookings": [
        ///       {
        ///         "bookingId": 45,
        ///         "startTime": "2024-10-20T08:00:00Z",
        ///         "endTime": "2024-10-20T18:00:00Z",
        ///         "durationHours": 10.0,
        ///         "distanceTravelled": 120,
        ///         "status": "Completed",
        ///         "purpose": "Business trip"
        ///       }
        ///     ],
        ///     "analysisStartDate": "2024-01-01T00:00:00Z",
        ///     "analysisEndDate": "2024-10-23T00:00:00Z",
        ///     "generatedAt": "2024-10-23T10:30:00Z"
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Co-owner usage detail retrieved successfully</response>
        /// <response code="403">NOT_AUTHORIZED_TO_VIEW_VEHICLE_ANALYTICS - User is not a co-owner of this vehicle</response>
        /// <response code="404">CO_OWNER_NOT_FOUND - Co-owner not found or not associated with vehicle</response>
        /// <response code="500">INTERNAL_SERVER_ERROR - Server error occurred</response>
        [HttpGet("vehicle/{vehicleId}/co-owner/{coOwnerId}/usage-detail")]
        [ProducesResponseType(typeof(BaseResponse<CoOwnerUsageDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<CoOwnerUsageDetailResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<CoOwnerUsageDetailResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCoOwnerUsageDetail(
            int vehicleId,
            int coOwnerId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var response = await _usageAnalyticsService.GetCoOwnerUsageDetailAsync(
                vehicleId, coOwnerId, userId, startDate, endDate);

            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// **[CoOwner/User]** Get personal usage history across all vehicles
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Returns comprehensive personal usage history for the current user across all vehicles they co-own.
        /// Includes paginated booking list, statistics, vehicle breakdown, and period analysis.
        /// 
        /// **Parameters:**
        /// - `startDate` (query, optional): History start date (default: 1 year ago)
        /// - `endDate` (query, optional): History end date (default: current date)
        /// - `vehicleId` (query, optional): Filter by specific vehicle ID (default: all vehicles)
        /// - `status` (query, optional): Filter by status - "All" (default), "Completed", "Cancelled", "Pending"
        /// - `pageNumber` (query): Page number for pagination (default: 1)
        /// - `pageSize` (query): Items per page (default: 20, max: 100)
        /// - `sortBy` (query): Sort field - "StartTime" (default), "EndTime", "DurationHours", "Distance"
        /// - `sortOrder` (query): Sort direction - "desc" (default), "asc"
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/usageanalytics/my/usage-history?pageNumber=1&amp;pageSize=20&amp;status=Completed
        /// Authorization: Bearer {token}
        /// ```
        /// 
        /// **Sample Response (200 OK):**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "Personal usage history retrieved successfully",
        ///   "data": {
        ///     "userId": 5,
        ///     "userName": "John Doe",
        ///     "email": "john@example.com",
        ///     "summary": {
        ///       "totalVehicles": 3,
        ///       "totalBookings": 45,
        ///       "completedBookings": 40,
        ///       "cancelledBookings": 3,
        ///       "pendingBookings": 2,
        ///       "totalHoursUsed": 320.50,
        ///       "totalDistanceTraveled": 2450,
        ///       "averageBookingDuration": 8.01,
        ///       "averageTripDistance": 61.25,
        ///       "mostActiveDay": "Monday",
        ///       "mostActiveTimeSlot": "Morning",
        ///       "favoriteVehicleId": 1,
        ///       "favoriteVehicleName": "Tesla Model 3",
        ///       "favoriteVehicleBookingCount": 25
        ///     },
        ///     "bookings": [...],
        ///     "pagination": {
        ///       "currentPage": 1,
        ///       "pageSize": 20,
        ///       "totalPages": 3,
        ///       "totalItems": 45,
        ///       "hasPreviousPage": false,
        ///       "hasNextPage": true
        ///     }
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Personal usage history retrieved successfully</response>
        /// <response code="404">USER_NOT_CO_OWNER - User is not a co-owner of any vehicle</response>
        /// <response code="500">INTERNAL_SERVER_ERROR - Server error occurred</response>
        [HttpGet("my/usage-history")]
        [ProducesResponseType(typeof(BaseResponse<PersonalUsageHistoryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<PersonalUsageHistoryResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPersonalUsageHistory([FromQuery] GetPersonalUsageHistoryRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var response = await _usageAnalyticsService.GetPersonalUsageHistoryAsync(userId, request);

            return response.StatusCode switch
            {
                200 => Ok(response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// **[CoOwner]** Get comprehensive group usage summary for a vehicle
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Returns comprehensive group usage summary including all co-owners' contributions,
        /// usage distribution patterns, popular time slots, and vehicle utilization metrics.
        /// 
        /// **Parameters:**
        /// - `vehicleId` (query): Vehicle ID to analyze
        /// - `startDate` (query, optional): Analysis start date (default: vehicle creation)
        /// - `endDate` (query, optional): Analysis end date (default: current date)
        /// - `includeTimeBreakdown` (query): Include time period breakdown (default: true)
        /// - `granularity` (query): Time breakdown granularity - "Monthly" (default), "Weekly", "Daily"
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/usageanalytics/group-summary?vehicleId=1&amp;granularity=Monthly
        /// Authorization: Bearer {token}
        /// ```
        /// 
        /// **Sample Response (200 OK):**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "Group usage summary retrieved successfully",
        ///   "data": {
        ///     "vehicleId": 1,
        ///     "vehicleName": "Tesla Model 3",
        ///     "licensePlate": "30A-12345",
        ///     "groupStats": {
        ///       "totalCoOwners": 3,
        ///       "totalBookings": 120,
        ///       "completedBookings": 105,
        ///       "cancelledBookings": 10,
        ///       "activeCoOwners": 3,
        ///       "totalHoursUsed": 840.50,
        ///       "totalDistanceTraveled": 6300,
        ///       "averageHoursPerBooking": 8.00,
        ///       "averageDistancePerTrip": 60.00,
        ///       "utilizationRate": 35.20,
        ///       "averageBookingsPerCoOwner": 40.00,
        ///       "fairnessScore": 85.50
        ///     },
        ///     "coOwners": [...],
        ///     "distribution": {
        ///       "distributionVariance": 12.50,
        ///       "distributionPattern": "Varied",
        ///       "mostActiveCoOwner": {...},
        ///       "leastActiveCoOwner": {...}
        ///     },
        ///     "popularTimeSlots": [
        ///       {
        ///         "timeSlot": "Monday Morning",
        ///         "bookingCount": 15,
        ///         "percentageOfTotal": 12.50,
        ///         "averageDuration": 8.5
        ///       }
        ///     ],
        ///     "utilization": {
        ///       "utilizationPercentage": 35.20,
        ///       "averageBookingsPerDay": 0.50,
        ///       "idleDays": 155,
        ///       "idlePercentage": 64.80
        ///     }
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Group usage summary retrieved successfully</response>
        /// <response code="403">NOT_AUTHORIZED_TO_VIEW_VEHICLE_ANALYTICS - User is not a co-owner</response>
        /// <response code="404">VEHICLE_NOT_FOUND - Vehicle not found</response>
        /// <response code="500">INTERNAL_SERVER_ERROR - Server error occurred</response>
        [HttpGet("group-summary")]
        [ProducesResponseType(typeof(BaseResponse<GroupUsageSummaryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<GroupUsageSummaryResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<GroupUsageSummaryResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetGroupUsageSummary([FromQuery] GetGroupUsageSummaryRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var response = await _usageAnalyticsService.GetGroupUsageSummaryAsync(userId, request);

            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }
    }
}
