namespace EvCoOwnership.DTOs.Notifications
{
    /// <summary>
    /// Event data for notification events communicated through the application
    /// </summary>
    public class NotificationEventData
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public string NotificationType { get; set; } = string.Empty;
        public string? AdditionalData { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}