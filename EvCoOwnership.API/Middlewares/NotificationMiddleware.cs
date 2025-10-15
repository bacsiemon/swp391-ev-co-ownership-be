using EvCoOwnership.API.Hubs;
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
        private async void OnNotificationCreated(object? sender, NotificationEventArgs e)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub, INotificationClient>>();

                var eventData = e.EventData;
                
                // Create notification response DTO
                var notificationDto = new NotificationResponseDto
                {
                    Id = eventData.NotificationId,
                    NotificationType = eventData.NotificationType,
                    Priority = GetPriorityFromNotification(eventData),
                    AdditionalData = eventData.AdditionalData,
                    CreatedAt = eventData.CreatedAt,
                    IsRead = false,
                    ReadAt = null
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
        }

        /// <summary>
        /// Gets priority enum from event data
        /// Note: This is a simple approach. In a real scenario, you might want to store priority in the event data
        /// </summary>
        /// <param name="eventData">Event data</param>
        /// <returns>Priority level</returns>
        private static EvCoOwnership.Repositories.Enums.ESeverityType GetPriorityFromNotification(NotificationEventData eventData)
        {
            // Default to Medium priority
            // In a real implementation, you might want to determine priority based on notification type
            // or include priority in the event data
            return eventData.NotificationType.ToLower() switch
            {
                var type when type.Contains("urgent") || type.Contains("critical") => EvCoOwnership.Repositories.Enums.ESeverityType.Critical,
                var type when type.Contains("high") || type.Contains("important") => EvCoOwnership.Repositories.Enums.ESeverityType.High,
                var type when type.Contains("low") || type.Contains("info") => EvCoOwnership.Repositories.Enums.ESeverityType.Low,
                _ => EvCoOwnership.Repositories.Enums.ESeverityType.Medium
            };
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