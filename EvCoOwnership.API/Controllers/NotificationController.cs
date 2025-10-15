using EvCoOwnership.API.Attributes;
using EvCoOwnership.DTOs.Notifications;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Controller for notification management operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Admin: Gets all notifications for the current user with pagination
        /// User: Gets all notifications for the current user with pagination
        /// </summary>
        /// <remarks>
        /// **Parameters:**
        /// - pageIndex: Page number (starts from 1, default: 1)
        /// - pageSize: Items per page (default: 10, max: 50)
        /// - includeRead: Include read notifications (default: true)
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/notification/my-notifications?pageIndex=1&pageSize=10&includeRead=true
        /// Authorization: Bearer {token}
        /// ```
        /// 
        /// **Sample Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "Notifications retrieved successfully",
        ///   "data": {
        ///     "items": [
        ///       {
        ///         "id": 1,
        ///         "notificationType": "Vehicle Booking Approved",
        ///         "priority": 1,
        ///         "additionalData": "{\"vehicleId\": 123, \"bookingId\": 456}",
        ///         "createdAt": "2025-10-13T10:30:00Z",
        ///         "isRead": false,
        ///         "readAt": null
        ///       }
        ///     ],
        ///     "pageIndex": 1,
        ///     "pageSize": 10,
        ///     "totalCount": 25,
        ///     "totalPages": 3,
        ///     "hasPreviousPage": false,
        ///     "hasNextPage": true
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Notifications retrieved successfully</response>
        /// <response code="401">Unauthorized - Invalid or missing token</response>
        /// <response code="400">Bad request - Invalid parameters</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("my-notifications")]
        public async Task<IActionResult> GetMyNotifications(
            [FromQuery] int pageIndex = 1, 
            [FromQuery] int pageSize = 10, 
            [FromQuery] bool includeRead = true)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                // Validate parameters
                if (pageIndex < 1) pageIndex = 1;
                if (pageSize < 1 || pageSize > 50) pageSize = 10;

                var response = await _notificationService.GetUserNotificationsAsync(userId.Value, pageIndex, pageSize, includeRead);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    404 => NotFound(response),
                    400 => BadRequest(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications for user");
                return StatusCode(500, new { message = "An error occurred while retrieving notifications" });
            }
        }

        /// <summary>
        /// Admin: Gets unread notification count for the current user
        /// User: Gets unread notification count for the current user
        /// </summary>
        /// <remarks>
        /// **Sample Request:**
        /// ```
        /// GET /api/notification/unread-count
        /// Authorization: Bearer {token}
        /// ```
        /// 
        /// **Sample Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "Unread count retrieved successfully",
        ///   "data": 5
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Unread count retrieved successfully</response>
        /// <response code="401">Unauthorized - Invalid or missing token</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                var response = await _notificationService.GetUnreadCountAsync(userId.Value);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for user");
                return StatusCode(500, new { message = "An error occurred while retrieving unread count" });
            }
        }

        /// <summary>
        /// User: Marks a single notification as read
        /// Admin: Marks a single notification as read
        /// </summary>
        /// <remarks>
        /// **Parameters:**
        /// - userNotificationId: ID of the user notification to mark as read
        /// 
        /// **Sample Request:**
        /// ```
        /// PUT /api/notification/mark-read/123
        /// Authorization: Bearer {token}
        /// ```
        /// 
        /// **Sample Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "Notification marked as read successfully",
        ///   "data": true
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Notification marked as read successfully</response>
        /// <response code="401">Unauthorized - Invalid or missing token</response>
        /// <response code="404">Notification not found or doesn't belong to user</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("mark-read/{userNotificationId}")]
        public async Task<IActionResult> MarkNotificationAsRead(int userNotificationId)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                var response = await _notificationService.MarkNotificationAsReadAsync(userId.Value, userNotificationId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return StatusCode(500, new { message = "An error occurred while marking notification as read" });
            }
        }

        /// <summary>
        /// User: Marks multiple notifications as read
        /// Admin: Marks multiple notifications as read
        /// </summary>
        /// <remarks>
        /// **Parameters:**
        /// - request: List of user notification IDs to mark as read
        /// 
        /// **Sample Request:**
        /// ```
        /// PUT /api/notification/mark-multiple-read
        /// Authorization: Bearer {token}
        /// Content-Type: application/json
        /// 
        /// {
        ///   "notificationIds": [1, 2, 3, 4, 5]
        /// }
        /// ```
        /// 
        /// **Sample Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "3 notifications marked as read successfully",
        ///   "data": 3
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Notifications marked as read successfully</response>
        /// <response code="401">Unauthorized - Invalid or missing token</response>
        /// <response code="400">Bad request - Invalid notification IDs</response>
        /// <response code="404">No valid notifications found for this user</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("mark-multiple-read")]
        public async Task<IActionResult> MarkMultipleNotificationsAsRead([FromBody] MarkNotificationReadRequestDto request)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                var response = await _notificationService.MarkMultipleNotificationsAsReadAsync(userId.Value, request.NotificationIds);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    404 => NotFound(response),
                    400 => BadRequest(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking multiple notifications as read");
                return StatusCode(500, new { message = "An error occurred while marking notifications as read" });
            }
        }

        /// <summary>
        /// User: Marks all unread notifications as read
        /// Admin: Marks all unread notifications as read
        /// </summary>
        /// <remarks>
        /// **Sample Request:**
        /// ```
        /// PUT /api/notification/mark-all-read
        /// Authorization: Bearer {token}
        /// ```
        /// 
        /// **Sample Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "12 notifications marked as read successfully",
        ///   "data": 12
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">All notifications marked as read successfully</response>
        /// <response code="401">Unauthorized - Invalid or missing token</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                var response = await _notificationService.MarkAllNotificationsAsReadAsync(userId.Value);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(500, new { message = "An error occurred while marking all notifications as read" });
            }
        }

        /// <summary>
        /// Admin: Manually sends a notification to a specific user
        /// </summary>
        /// <remarks>
        /// **Parameters:**
        /// - request: Notification data including user ID, message, and type
        /// 
        /// **Sample Request:**
        /// ```
        /// POST /api/notification/send-to-user
        /// Authorization: Bearer {token}
        /// Content-Type: application/json
        /// 
        /// {
        ///   "userId": 123,
        ///   "message": "Your vehicle booking has been approved",
        ///   "notificationType": "Booking Approval",
        ///   "additionalData": "{\"bookingId\": 456, \"vehicleId\": 789}"
        /// }
        /// ```
        /// 
        /// **Sample Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "Notification sent successfully to 1 users",
        ///   "data": 25
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Notification sent successfully</response>
        /// <response code="401">Unauthorized - Invalid or missing token</response>
        /// <response code="403">Forbidden - Admin role required</response>
        /// <response code="400">Bad request - Invalid user ID or missing data</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("send-to-user")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendNotificationToUser([FromBody] SendNotificationRequestDto request)
        {
            try
            {
                var response = await _notificationService.SendNotificationToUserAsync(
                    request.UserId, 
                    request.NotificationType, 
                    ESeverityType.Medium, 
                    request.AdditionalData);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    400 => BadRequest(response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to user");
                return StatusCode(500, new { message = "An error occurred while sending notification" });
            }
        }

        /// <summary>
        /// Admin: Manually creates and sends a notification to multiple users
        /// </summary>
        /// <remarks>
        /// **Parameters:**
        /// - request: Notification data including user IDs, type, and priority
        /// 
        /// **Sample Request:**
        /// ```
        /// POST /api/notification/create-notification
        /// Authorization: Bearer {token}
        /// Content-Type: application/json
        /// 
        /// {
        ///   "notificationType": "System Maintenance",
        ///   "priority": 2,
        ///   "userIds": [1, 2, 3, 4, 5],
        ///   "additionalData": "{\"maintenanceWindow\": \"2025-10-15T02:00:00Z\"}"
        /// }
        /// ```
        /// 
        /// **Sample Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "Notification sent successfully to 5 users",
        ///   "data": 26
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Notification created and sent successfully</response>
        /// <response code="401">Unauthorized - Invalid or missing token</response>
        /// <response code="403">Forbidden - Admin role required</response>
        /// <response code="400">Bad request - Invalid user IDs or missing data</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("create-notification")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationRequestDto request)
        {
            try
            {
                var response = await _notificationService.SendNotificationToUsersAsync(
                    request.NotificationType, 
                    request.Priority, 
                    request.UserIds, 
                    request.AdditionalData);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    400 => BadRequest(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                return StatusCode(500, new { message = "An error occurred while creating notification" });
            }
        }

        /// <summary>
        /// Extracts user ID from JWT claims
        /// </summary>
        /// <returns>User ID if found, null otherwise</returns>
        private int? GetUserIdFromClaims()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                              User.FindFirst("UserId")?.Value ??
                              User.FindFirst("sub")?.Value;
            
            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            return null;
        }
    }
}