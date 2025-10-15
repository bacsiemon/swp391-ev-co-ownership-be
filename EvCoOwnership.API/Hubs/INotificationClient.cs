using EvCoOwnership.DTOs.Notifications;

namespace EvCoOwnership.API.Hubs
{
    /// <summary>
    /// Strongly typed interface for NotificationHub client methods
    /// </summary>
    public interface INotificationClient
    {
        /// <summary>
        /// Called when a new notification is received
        /// </summary>
        /// <param name="notification">Notification data</param>
        Task ReceiveNotification(NotificationResponseDto notification);

        /// <summary>
        /// Called when notification read status is updated
        /// </summary>
        /// <param name="notificationId">ID of the notification</param>
        /// <param name="isRead">Read status</param>
        Task NotificationReadStatusChanged(int notificationId, bool isRead);

        /// <summary>
        /// Called when unread count is updated
        /// </summary>
        /// <param name="unreadCount">New unread count</param>
        Task UnreadCountChanged(int unreadCount);
    }
}