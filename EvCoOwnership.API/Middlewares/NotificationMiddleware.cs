using EvCoOwnership.API.Hubs;
using EvCoOwnership.API.Hubs.Clients;
using EvCoOwnership.DTOs.Notifications;
using EvCoOwnership.Services.Events;
using Microsoft.AspNetCore.SignalR;

namespace EvCoOwnership.API.Middlewares
{
    /// <summary>
    /// Middleware to listen for notification events and broadcast them via SignalR
    /// </summary>
    public class NotificationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationMiddleware> _logger;

        public NotificationMiddleware(RequestDelegate next, IServiceProvider serviceProvider, ILogger<NotificationMiddleware> logger)
        {
            _next = next;
            _serviceProvider = serviceProvider;
            _logger = logger;

            // Subscribe to notification events
            NotificationEventPublisher.NotificationCreated += OnNotificationCreated;
        }

        /// <summary>
        /// Middleware execution
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <returns>Task representing the async operation</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);
        }

        /// <summary>
        /// Handles notification created events and broadcasts via SignalR
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments containing notification data</param>
        private void OnNotificationCreated(object? sender, NotificationEventArgs e)
        {
            // Fire and forget approach to avoid blocking the main thread
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub, INotificationClient>>();

                    var eventData = e.EventData;

                    // Create user notification response DTO (since this is for a specific user)
                    var notificationDto = new UserNotificationResponseDto
                    {
                        NotificationId = eventData.NotificationId,
                        UserId = eventData.UserId,
                        NotificationType = eventData.NotificationType,
                        AdditionalData = eventData.AdditionalData,
                        CreatedAt = eventData.CreatedAt,
                        ReadAt = null // New notification, not read yet
                    };

                    // Send notification to the specific user's group
                    var userGroup = $"User_{eventData.UserId}";
                    await hubContext.Clients.Group(userGroup).ReceiveNotification(notificationDto);

                    _logger.LogInformation("Notification {NotificationId} broadcasted to user {UserId} via SignalR",
                        eventData.NotificationId, eventData.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error broadcasting notification {NotificationId} to user {UserId} via SignalR",
                        e.EventData.NotificationId, e.EventData.UserId);
                }
            });
        }
    }

    /// <summary>
    /// Extension method to register the notification middleware
    /// </summary>
    public static class NotificationMiddlewareExtensions
    {
        /// <summary>
        /// Adds the notification middleware to the pipeline
        /// </summary>
        /// <param name="app">Application builder</param>
        /// <returns>Application builder</returns>
        public static IApplicationBuilder UseNotificationMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<NotificationMiddleware>();
        }
    }
}