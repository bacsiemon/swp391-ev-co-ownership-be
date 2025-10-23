using EvCoOwnership.API.Hubs.Clients;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace EvCoOwnership.API.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time notification delivery
    /// Admin/User: Connects to hub for real-time notifications
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub<INotificationClient>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(INotificationService notificationService, ILogger<NotificationHub> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Called when client connects to hub
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        public override async Task OnConnectedAsync()
        {
            var userId = GetUserIdFromContext();
            if (userId.HasValue)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId.Value}");
                _logger.LogInformation("User {UserId} connected with connection {ConnectionId}", userId.Value, Context.ConnectionId);
                
                // Send current unread count when user connects
                var unreadCountResponse = await _notificationService.GetUnreadCountAsync(userId.Value);
                if (unreadCountResponse.StatusCode == 200)
                {
                    await Clients.Caller.UnreadCountChanged(unreadCountResponse.Data);
                }
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when client disconnects from hub
        /// </summary>
        /// <param name="exception">Exception that caused disconnection, if any</param>
        /// <returns>Task representing the async operation</returns>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserIdFromContext();
            if (userId.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId.Value}");
                _logger.LogInformation("User {UserId} disconnected from connection {ConnectionId}", userId.Value, Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Client method to mark a notification as read
        /// </summary>
        /// <param name="userNotificationId">User notification ID to mark as read</param>
        /// <returns>Task representing the async operation</returns>
        public async Task MarkNotificationAsRead(int userNotificationId)
        {
            var userId = GetUserIdFromContext();
            if (!userId.HasValue)
            {
                _logger.LogWarning("Unauthorized attempt to mark notification as read");
                return;
            }

            try
            {
                var request = new EvCoOwnership.DTOs.Notifications.MarkNotificationAsReadRequest
                {
                    UserNotificationId = userNotificationId
                };

                var result = await _notificationService.MarkNotificationAsReadAsync(userId.Value, request);
                
                if (result.StatusCode == 200 && result.Data)
                {
                    // Notify the client that the notification read status changed
                    await Clients.Caller.NotificationReadStatusChanged(userNotificationId, true);
                    
                    // Send updated unread count
                    var unreadCountResponse = await _notificationService.GetUnreadCountAsync(userId.Value);
                    if (unreadCountResponse.StatusCode == 200)
                    {
                        await Clients.Caller.UnreadCountChanged(unreadCountResponse.Data);
                    }

                    _logger.LogInformation("User {UserId} marked notification {NotificationId} as read", userId.Value, userNotificationId);
                }
                else
                {
                    _logger.LogWarning("Failed to mark notification {NotificationId} as read for user {UserId}: {Message}", 
                        userNotificationId, userId.Value, result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read for user {UserId}", 
                    userNotificationId, userId.Value);
            }
        }

        /// <summary>
        /// Client method to mark multiple notifications as read
        /// </summary>
        /// <param name="userNotificationIds">List of user notification IDs to mark as read</param>
        /// <returns>Task representing the async operation</returns>
        public async Task MarkMultipleNotificationsAsRead(List<int> userNotificationIds)
        {
            var userId = GetUserIdFromContext();
            if (!userId.HasValue)
            {
                _logger.LogWarning("Unauthorized attempt to mark multiple notifications as read");
                return;
            }

            try
            {
                var request = new EvCoOwnership.DTOs.Notifications.MarkMultipleNotificationsAsReadRequest
                {
                    UserNotificationIds = userNotificationIds
                };

                var result = await _notificationService.MarkMultipleNotificationsAsReadAsync(userId.Value, request);
                
                if (result.StatusCode == 200 && result.Data > 0)
                {
                    // Notify the client that notifications read status changed
                    foreach (var notificationId in userNotificationIds)
                    {
                        await Clients.Caller.NotificationReadStatusChanged(notificationId, true);
                    }
                    
                    // Send updated unread count
                    var unreadCountResponse = await _notificationService.GetUnreadCountAsync(userId.Value);
                    if (unreadCountResponse.StatusCode == 200)
                    {
                        await Clients.Caller.UnreadCountChanged(unreadCountResponse.Data);
                    }

                    _logger.LogInformation("User {UserId} marked {Count} notifications as read", userId.Value, result.Data);
                }
                else
                {
                    _logger.LogWarning("Failed to mark multiple notifications as read for user {UserId}: {Message}", 
                        userId.Value, result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking multiple notifications as read for user {UserId}", userId.Value);
            }
        }

        /// <summary>
        /// Client method to mark all notifications as read
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        public async Task MarkAllNotificationsAsRead()
        {
            var userId = GetUserIdFromContext();
            if (!userId.HasValue)
            {
                _logger.LogWarning("Unauthorized attempt to mark all notifications as read");
                return;
            }

            try
            {
                var result = await _notificationService.MarkAllNotificationsAsReadAsync(userId.Value);
                
                if (result.StatusCode == 200)
                {
                    // Send updated unread count (should be 0)
                    await Clients.Caller.UnreadCountChanged(0);

                    _logger.LogInformation("User {UserId} marked all notifications as read. Count: {Count}", userId.Value, result.Data);
                }
                else
                {
                    _logger.LogWarning("Failed to mark all notifications as read for user {UserId}: {Message}", 
                        userId.Value, result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId.Value);
            }
        }

        /// <summary>
        /// Client method to get current unread count
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        public async Task GetUnreadCount()
        {
            var userId = GetUserIdFromContext();
            if (!userId.HasValue)
            {
                _logger.LogWarning("Unauthorized attempt to get unread count");
                return;
            }

            try
            {
                var result = await _notificationService.GetUnreadCountAsync(userId.Value);
                
                if (result.StatusCode == 200)
                {
                    await Clients.Caller.UnreadCountChanged(result.Data);
                }
                else
                {
                    _logger.LogWarning("Failed to get unread count for user {UserId}: {Message}", userId.Value, result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for user {UserId}", userId.Value);
            }
        }

        /// <summary>
        /// Gets the user ID from the current connection context
        /// </summary>
        /// <returns>User ID if found, null otherwise</returns>
        private int? GetUserIdFromContext()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                              Context.User?.FindFirst("UserId")?.Value ??
                              Context.User?.FindFirst("sub")?.Value;
            
            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            _logger.LogWarning("Unable to extract user ID from connection context for connection {ConnectionId}", Context.ConnectionId);
            return null;
        }
    }
}