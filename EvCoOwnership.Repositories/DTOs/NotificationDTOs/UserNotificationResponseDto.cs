using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.DTOs.Notifications
{
    /// <summary>
    /// Response DTO for user-specific notification data (combines Notification and UserNotification)
    /// </summary>
    public class UserNotificationResponseDto
    {
        public int Id { get; set; }
        public int? NotificationId { get; set; }
        public int? UserId { get; set; }
        public DateTime? ReadAt { get; set; }
        
        // Notification details
        public string NotificationType { get; set; }
        public string? AdditionalData { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}