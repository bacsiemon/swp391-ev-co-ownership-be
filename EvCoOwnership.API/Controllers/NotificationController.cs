using EvCoOwnership.API.Attributes;
using EvCoOwnership.DTOs.Notifications;
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

        /// <summary>User</summary>
        /// <remarks>
        /// Gets all notifications for the current user with pagination.
        /// 
        /// Parameters:
        /// - pageIndex: Page number (starts from 1, default: 1)
        /// - pageSize: Items per page (default: 10, max: 50)
        /// - includeRead: Include read notifications (default: true)
        /// 
        /// Sample request:
        /// 
        /// GET /api/notification/my-notifications?pageIndex=1&pageSize=10&includeRead=true
        /// Authorization: Bearer {token}
        /// </remarks>
        /// <response code="200">Notifications retrieved successfully</response>
        /// <response code="400">Bad request - Invalid parameters</response>
        /// <response code="401">Unauthorized - Invalid or missing token</response>
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

        /// <summary>User</summary>
        /// <remarks>
        /// Gets unread notification count for the current user.
        /// 
        /// Sample request:
        /// 
        /// GET /api/notification/unread-count
        /// Authorization: Bearer {token}
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

        /// <summary>User</summary>
        /// <remarks>
        /// Marks a single notification as read.
        /// 
        /// Parameters:
        /// - request: Request DTO containing the user notification ID to mark as read
        /// 
        /// Sample request:
        /// 
        /// PUT /api/notification/mark-read
        /// Authorization: Bearer {token}
        /// Content-Type: application/json
        /// 
        /// {
        ///   "userNotificationId": 123
        /// }
        /// </remarks>
        /// <response code="200">Notification marked as read successfully</response>
        /// <response code="401">Unauthorized - Invalid or missing token</response>
        /// <response code="404">Notification not found or doesn't belong to user</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("mark-read")]
        public async Task<IActionResult> MarkNotificationAsRead([FromBody] MarkNotificationAsReadRequest request)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                var response = await _notificationService.MarkNotificationAsReadAsync(userId.Value, request);

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

        /// <summary>User</summary>
        /// <remarks>
        /// Marks multiple notifications as read.
        /// 
        /// Parameters:
        /// - request: Request DTO containing list of user notification IDs to mark as read
        /// 
        /// Sample request:
        /// 
        /// PUT /api/notification/mark-multiple-read
        /// Authorization: Bearer {token}
        /// Content-Type: application/json
        /// 
        /// {
        ///   "userNotificationIds": [1, 2, 3, 4, 5]
        /// }
        /// </remarks>
        /// <response code="200">Notifications marked as read successfully</response>
        /// <response code="400">Bad request - Invalid notification IDs</response>
        /// <response code="401">Unauthorized - Invalid or missing token</response>
        /// <response code="404">No valid notifications found for this user</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("mark-multiple-read")]
        public async Task<IActionResult> MarkMultipleNotificationsAsRead([FromBody] MarkMultipleNotificationsAsReadRequest request)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                var response = await _notificationService.MarkMultipleNotificationsAsReadAsync(userId.Value, request);

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

        /// <summary>User</summary>
        /// <remarks>
        /// Marks all unread notifications as read.
        /// 
        /// Sample request:
        /// 
        /// PUT /api/notification/mark-all-read
        /// Authorization: Bearer {token}
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

        /// <summary>Admin</summary>
        /// <remarks>
        /// Manually sends a notification to a specific user.
        /// 
        /// Parameters:
        /// - request: Notification data including user ID, message, and type
        /// 
        /// Sample request:
        /// 
        /// POST /api/notification/send-to-user
        /// Authorization: Bearer {token}
        /// Content-Type: application/json
        /// 
        /// {
        ///   "userId": 123,
        ///   "notificationType": "Booking",
        ///   "additionalData": "{\"bookingId\": 456, \"vehicleId\": 789}"
        /// }
        /// </remarks>
        /// <response code="200">Notification sent successfully</response>
        /// <response code="400">Bad request - Invalid user ID or missing data</response>
        /// <response code="401">Unauthorized - Invalid or missing token</response>
        /// <response code="403">Forbidden - Admin role required</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("send-to-user")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendNotificationToUser([FromBody] SendNotificationRequestDto request)
        {
            try
            {
                var response = await _notificationService.SendNotificationToUserAsync(request);

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

        /// <summary>Admin</summary>
        /// <remarks>
        /// Manually creates and sends a notification to multiple users.
        /// 
        /// Parameters:
        /// - request: Notification data including user IDs, type, and priority
        /// 
        /// Sample request:
        /// 
        /// POST /api/notification/create-notification
        /// Authorization: Bearer {token}
        /// Content-Type: application/json
        /// 
        /// {
        ///   "notificationType": "System",
        ///   "userIds": [1, 2, 3, 4, 5],
        ///   "additionalData": "{\"maintenanceWindow\": \"2025-10-15T02:00:00Z\"}"
        /// }
        /// </remarks>
        /// <response code="200">Notification created and sent successfully</response>
        /// <response code="400">Bad request - Invalid user IDs or missing data</response>
        /// <response code="401">Unauthorized - Invalid or missing token</response>
        /// <response code="403">Forbidden - Admin role required</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("create-notification")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationRequest request)
        {
            try
            {
                var response = await _notificationService.SendNotificationToUsersAsync(request);

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