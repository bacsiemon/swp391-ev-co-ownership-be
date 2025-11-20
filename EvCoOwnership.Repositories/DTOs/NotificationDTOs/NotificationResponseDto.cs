using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Repositories.DTOs.NotificationDTOs
{
    /// <summary>
    /// Response DTO for notification data
    /// </summary>
    public class NotificationResponseDto
    {
        public int Id { get; set; }
        public string NotificationType { get; set; }
        public string? AdditionalData { get; set; }
        public DateTime? CreatedAt { get; set; }
        // Note: IsRead and ReadAt belong to UserNotification, not Notification
        // These should be included in a separate UserNotificationResponseDto if needed
    }
}