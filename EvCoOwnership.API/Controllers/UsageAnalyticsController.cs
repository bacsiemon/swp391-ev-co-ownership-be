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

        /// <summary>
        /// **[CoOwner]** Compare usage across multiple co-owners over time
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Returns time-series comparison of co-owners' usage with trend analysis, rankings, 
        /// and fairness insights. Shows who is using the vehicle more/less over time.
        /// 
        /// **Parameters:**
        /// - `vehicleId` (query): Vehicle ID to analyze
        /// - `startDate` (query, optional): Comparison start date (default: 3 months ago)
        /// - `endDate` (query, optional): Comparison end date (default: current date)
        /// - `granularity` (query): Time granularity - "Weekly" (default), "Daily", "Monthly"
        /// - `metrics` (query): Metrics to compare - "All" (default), "Hours", "Distance", "BookingCount"
        /// - `coOwnerIds` (query, optional): Specific co-owner IDs to compare (compares all if empty)
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/usageanalytics/compare/co-owners?vehicleId=1&amp;granularity=Weekly
        /// Authorization: Bearer {token}
        /// ```
        /// 
        /// **Sample Response (200 OK):**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "Co-owners usage comparison retrieved successfully",
        ///   "data": {
        ///     "vehicleId": 1,
        ///     "vehicleName": "Tesla Model 3",
        ///     "coOwnersSeries": [
        ///       {
        ///         "coOwnerId": 1,
        ///         "coOwnerName": "John Doe",
        ///         "email": "john@example.com",
        ///         "ownershipPercentage": 40.00,
        ///         "dataPoints": [
        ///           {
        ///             "periodStart": "2024-01-01T00:00:00Z",
        ///             "periodEnd": "2024-01-08T00:00:00Z",
        ///             "periodLabel": "Week 1 (Jan 01)",
        ///             "hours": 15.50,
        ///             "distance": 120,
        ///             "bookingCount": 3,
        ///             "utilizationRate": 9.23,
        ///             "hoursChange": 2.50,
        ///             "distanceChange": 20,
        ///             "bookingCountChange": 1
        ///           }
        ///         ],
        ///         "trend": {
        ///           "direction": "Increasing",
        ///           "growthRate": 25.50,
        ///           "averageChange": 1.20,
        ///           "volatility": 3.40,
        ///           "isConsistent": true,
        ///           "pattern": "Linear"
        ///         },
        ///         "totalHours": 180.00,
        ///         "totalDistance": 1500,
        ///         "totalBookings": 30,
        ///         "averagePerPeriod": 15.00,
        ///         "peakUsage": 22.50,
        ///         "peakPeriod": "Week 5 (Feb 01)"
        ///       }
        ///     ],
        ///     "statistics": {
        ///       "totalHoursAllCoOwners": 500.00,
        ///       "usageDispersion": 45.20,
        ///       "giniCoefficient": 0.285,
        ///       "mostActiveCoOwner": "John Doe",
        ///       "mostActiveHours": 200.00,
        ///       "fastestGrowingCoOwner": "Jane Smith",
        ///       "fastestGrowthRate": 35.50
        ///     },
        ///     "rankings": [
        ///       {
        ///         "metric": "Hours",
        ///         "rankings": [
        ///           {
        ///             "rank": 1,
        ///             "coOwnerId": 1,
        ///             "coOwnerName": "John Doe",
        ///             "value": 200.00,
        ///             "percentageOfTotal": 40.00
        ///           }
        ///         ]
        ///       }
        ///     ],
        ///     "insights": [
        ///       {
        ///         "type": "Imbalance",
        ///         "severity": "Warning",
        ///         "title": "Significant Usage Imbalance Detected",
        ///         "description": "Usage distribution shows significant inequality (Gini: 0.450). Some co-owners may be using the vehicle much more than others relative to their ownership.",
        ///         "affectedCoOwners": ["John Doe", "Jane Smith"]
        ///       }
        ///     ],
        ///     "granularity": "Weekly",
        ///     "totalPeriods": 12
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Co-owners usage comparison retrieved successfully</response>
        /// <response code="403">ACCESS_DENIED_NOT_CO_OWNER - User is not a co-owner</response>
        /// <response code="404">VEHICLE_NOT_FOUND or NO_CO_OWNERS_FOUND</response>
        /// <response code="500">ERROR_COMPARING_CO_OWNERS_USAGE - Server error occurred</response>
        [HttpGet("compare/co-owners")]
        [ProducesResponseType(typeof(BaseResponse<CoOwnersUsageComparisonResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<CoOwnersUsageComparisonResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<CoOwnersUsageComparisonResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CompareCoOwnersUsage([FromQuery] CompareCoOwnersUsageRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var response = await _usageAnalyticsService.CompareCoOwnersUsageAsync(userId, request);

            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// **[CoOwner]** Compare usage across multiple vehicles over time
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Returns time-series comparison of multiple vehicles' usage with trend analysis, 
        /// utilization rankings, and efficiency insights. Useful for comparing performance 
        /// across different vehicles in user's portfolio.
        /// 
        /// **Parameters:**
        /// - `vehicleIds` (query): List of vehicle IDs to compare (minimum 2, comma-separated)
        /// - `startDate` (query, optional): Comparison start date (default: 3 months ago)
        /// - `endDate` (query, optional): Comparison end date (default: current date)
        /// - `granularity` (query): Time granularity - "Weekly" (default), "Daily", "Monthly"
        /// - `metrics` (query): Metrics to compare - "All" (default), "Hours", "Distance", "BookingCount", "UtilizationRate"
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/usageanalytics/compare/vehicles?vehicleIds=1,2,3&amp;granularity=Monthly
        /// Authorization: Bearer {token}
        /// ```
        /// 
        /// **Sample Response (200 OK):**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "Vehicles usage comparison retrieved successfully",
        ///   "data": {
        ///     "vehiclesSeries": [
        ///       {
        ///         "vehicleId": 1,
        ///         "vehicleName": "Tesla Model 3",
        ///         "licensePlate": "30A-12345",
        ///         "dataPoints": [
        ///           {
        ///             "periodStart": "2024-01-01T00:00:00Z",
        ///             "periodEnd": "2024-02-01T00:00:00Z",
        ///             "periodLabel": "Jan 2024",
        ///             "hours": 250.00,
        ///             "distance": 2000,
        ///             "bookingCount": 40,
        ///             "utilizationRate": 33.60,
        ///             "hoursChange": 20.00
        ///           }
        ///         ],
        ///         "trend": {
        ///           "direction": "Increasing",
        ///           "growthRate": 15.50,
        ///           "pattern": "Linear"
        ///         },
        ///         "totalHours": 800.00,
        ///         "totalDistance": 6500,
        ///         "totalBookings": 120,
        ///         "averageUtilization": 35.20,
        ///         "peakUtilization": 42.30,
        ///         "peakPeriod": "Mar 2024"
        ///       }
        ///     ],
        ///     "statistics": {
        ///       "totalHoursAllVehicles": 1800.00,
        ///       "averageUtilizationRate": 30.50,
        ///       "mostUtilizedVehicle": "Tesla Model 3",
        ///       "mostUtilizedHours": 800.00,
        ///       "mostEfficientVehicle": "Nissan Leaf",
        ///       "bestUtilizationRate": 38.70
        ///     },
        ///     "rankings": [
        ///       {
        ///         "metric": "Hours",
        ///         "rankings": [
        ///           {
        ///             "rank": 1,
        ///             "vehicleId": 1,
        ///             "vehicleName": "Tesla Model 3",
        ///             "value": 800.00,
        ///             "percentageOfTotal": 44.44
        ///           }
        ///         ]
        ///       },
        ///       {
        ///         "metric": "UtilizationRate",
        ///         "rankings": [...]
        ///       }
        ///     ],
        ///     "insights": [
        ///       {
        ///         "type": "Recommendation",
        ///         "severity": "Warning",
        ///         "title": "Underutilized Vehicles Detected",
        ///         "description": "1 vehicle(s) have low utilization rates (&lt;20%). Consider promoting their usage or reviewing ownership structure.",
        ///         "data": {
        ///           "vehicleCount": 1,
        ///           "avgUtilization": 18.50
        ///         }
        ///       }
        ///     ],
        ///     "granularity": "Monthly",
        ///     "totalPeriods": 3
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Vehicles usage comparison retrieved successfully</response>
        /// <response code="403">ACCESS_DENIED_NOT_CO_OWNER_OF_VEHICLE_{id} - User not co-owner of all vehicles</response>
        /// <response code="404">SOME_VEHICLES_NOT_FOUND - One or more vehicles not found</response>
        /// <response code="500">ERROR_COMPARING_VEHICLES_USAGE - Server error occurred</response>
        [HttpGet("compare/vehicles")]
        [ProducesResponseType(typeof(BaseResponse<VehiclesUsageComparisonResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<VehiclesUsageComparisonResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<VehiclesUsageComparisonResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CompareVehiclesUsage([FromQuery] CompareVehiclesUsageRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var response = await _usageAnalyticsService.CompareVehiclesUsageAsync(userId, request);

            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// **[User]** Compare personal usage between two time periods
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Returns detailed comparison of user's usage between two time periods,
        /// showing changes in usage patterns, metrics, and co-owner contributions.
        /// Useful for month-over-month or quarter-over-quarter analysis.
        /// 
        /// **Parameters:**
        /// - `vehicleId` (query, optional): Vehicle ID to analyze (analyzes all vehicles if null)
        /// - `period1Start` (query): First period start date
        /// - `period1End` (query): First period end date
        /// - `period2Start` (query): Second period start date
        /// - `period2End` (query): Second period end date
        /// - `period1Label` (query, optional): Custom label for period 1 (e.g., "Q1 2024")
        /// - `period2Label` (query, optional): Custom label for period 2 (e.g., "Q2 2024")
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/usageanalytics/compare/periods?period1Start=2024-01-01&amp;period1End=2024-01-31&amp;period2Start=2024-02-01&amp;period2End=2024-02-29&amp;period1Label=January&amp;period2Label=February
        /// Authorization: Bearer {token}
        /// ```
        /// 
        /// **Sample Response (200 OK):**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "Period usage comparison retrieved successfully",
        ///   "data": {
        ///     "period1": {
        ///       "startDate": "2024-01-01T00:00:00Z",
        ///       "endDate": "2024-01-31T00:00:00Z",
        ///       "label": "January",
        ///       "durationDays": 31,
        ///       "totalHours": 120.00,
        ///       "totalDistance": 900,
        ///       "totalBookings": 18,
        ///       "averageBookingDuration": 6.67,
        ///       "averageTripDistance": 50.00,
        ///       "utilizationRate": 16.13,
        ///       "mostActiveDay": "Saturday",
        ///       "mostActiveTimeSlot": "Morning"
        ///     },
        ///     "period2": {
        ///       "startDate": "2024-02-01T00:00:00Z",
        ///       "endDate": "2024-02-29T00:00:00Z",
        ///       "label": "February",
        ///       "durationDays": 29,
        ///       "totalHours": 150.00,
        ///       "totalDistance": 1200,
        ///       "totalBookings": 22,
        ///       "averageBookingDuration": 6.82,
        ///       "averageTripDistance": 54.55,
        ///       "utilizationRate": 21.55,
        ///       "mostActiveDay": "Sunday",
        ///       "mostActiveTimeSlot": "Afternoon"
        ///     },
        ///     "comparison": {
        ///       "hoursChange": 30.00,
        ///       "distanceChange": 300,
        ///       "bookingCountChange": 4,
        ///       "utilizationRateChange": 5.42,
        ///       "hoursChangePercentage": 25.00,
        ///       "distanceChangePercentage": 33.33,
        ///       "bookingCountChangePercentage": 22.22,
        ///       "utilizationRateChangePercentage": 33.60,
        ///       "hoursPerDayChange": 1.03,
        ///       "distancePerDayChange": 10.34,
        ///       "bookingsPerDayChange": 0.14,
        ///       "overallTrend": "Increased",
        ///       "trendStrength": "Moderate"
        ///     },
        ///     "vehicleComparison": {
        ///       "vehicleId": 1,
        ///       "vehicleName": "Tesla Model 3",
        ///       "coOwnerChanges": [
        ///         {
        ///           "coOwnerId": 1,
        ///           "coOwnerName": "John Doe",
        ///           "period1Hours": 50.00,
        ///           "period2Hours": 65.00,
        ///           "hoursChange": 15.00,
        ///           "hoursChangePercentage": 30.00,
        ///           "changeDirection": "Increased"
        ///         }
        ///       ],
        ///       "patternComparison": {
        ///         "period1DayDistribution": {
        ///           "Monday": 3,
        ///           "Saturday": 5
        ///         },
        ///         "period2DayDistribution": {
        ///           "Monday": 4,
        ///           "Sunday": 6
        ///         }
        ///       }
        ///     },
        ///     "insights": [
        ///       {
        ///         "type": "Recommendation",
        ///         "severity": "Warning",
        ///         "title": "Significant Usage Increased",
        ///         "description": "Usage has increased by 25.0% between periods. Monitor for potential booking conflicts.",
        ///         "data": {
        ///           "changePercentage": 25.00,
        ///           "period1Hours": 120.00,
        ///           "period2Hours": 150.00
        ///         }
        ///       }
        ///     ]
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Period usage comparison retrieved successfully</response>
        /// <response code="404">USER_NOT_CO_OWNER - User is not a co-owner of any vehicle</response>
        /// <response code="500">ERROR_COMPARING_PERIOD_USAGE - Server error occurred</response>
        [HttpGet("compare/periods")]
        [ProducesResponseType(typeof(BaseResponse<PeriodUsageComparisonResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<PeriodUsageComparisonResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ComparePeriodUsage([FromQuery] ComparePeriodUsageRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var response = await _usageAnalyticsService.ComparePeriodUsageAsync(userId, request);

            return response.StatusCode switch
            {
                200 => Ok(response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }
    }
}
