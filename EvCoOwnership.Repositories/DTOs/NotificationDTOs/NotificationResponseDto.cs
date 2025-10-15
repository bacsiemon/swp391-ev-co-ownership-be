using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.DTOs.Notifications
{
    /// <summary>
    /// Response DTO for notification data
    /// </summary>
    public class NotificationResponseDto
    {
        public int Id { get; set; }
        public string NotificationType { get; set; } = string.Empty;
        public ESeverityType Priority { get; set; }
        public string? AdditionalData { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}