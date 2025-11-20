using EvCoOwnership.Repositories.DTOs.NotificationDTOs;

namespace EvCoOwnership.Services.Events
{
    /// <summary>
    /// Event arguments for notification events
    /// </summary>
    public class NotificationEventArgs : EventArgs
    {
        public NotificationEventData EventData { get; set; }

        public NotificationEventArgs(NotificationEventData eventData)
        {
            EventData = eventData;
        }
    }

    /// <summary>
    /// Event publisher for notification events throughout the application
    /// </summary>
    public static class NotificationEventPublisher
    {
        /// <summary>
        /// Event fired when a new notification is created
        /// </summary>
        public static event EventHandler<NotificationEventArgs>? NotificationCreated;

        /// <summary>
        /// Publishes a notification created event
        /// </summary>
        /// <param name="eventData">Notification event data</param>
        public static void PublishNotificationCreated(NotificationEventData eventData)
        {
            NotificationCreated?.Invoke(null, new NotificationEventArgs(eventData));
        }
    }
}