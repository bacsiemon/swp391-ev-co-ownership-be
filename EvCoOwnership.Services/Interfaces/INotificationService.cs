using EvCoOwnership.DTOs.Notifications;
using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Services.Interfaces
{
    /// <summary>
    /// Service interface for notification operations
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Sends a notification to multiple users and fires notification event
        /// </summary>
        /// <param name="notificationType">Type of notification</param>
        /// <param name="priority">Priority level of notification</param>
        /// <param name="userIds">List of user IDs to send notification to</param>
        /// <param name="additionalData">Optional additional data in JSON format</param>
        /// <returns>Base response indicating success or failure</returns>
        Task<BaseResponse<int>> SendNotificationToUsersAsync(string notificationType, ESeverityType priority, List<int> userIds, string? additionalData = null);

        /// <summary>
        /// Sends a notification to a single user and fires notification event
        /// </summary>
        /// <param name="userId">User ID to send notification to</param>
        /// <param name="notificationType">Type of notification</param>
        /// <param name="priority">Priority level of notification</param>
        /// <param name="additionalData">Optional additional data in JSON format</param>
        /// <returns>Base response indicating success or failure</returns>
        Task<BaseResponse<int>> SendNotificationToUserAsync(int userId, string notificationType, ESeverityType priority, string? additionalData = null);

        /// <summary>
        /// Gets paginated notifications for a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="pageIndex">Page index (starts from 1)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="includeRead">Include read notifications</param>
        /// <returns>Paginated list of notification response DTOs</returns>
        Task<BaseResponse<PaginatedList<NotificationResponseDto>>> GetUserNotificationsAsync(int userId, int pageIndex = 1, int pageSize = 10, bool includeRead = true);

        /// <summary>
        /// Marks a single notification as read for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="userNotificationId">User notification ID to mark as read</param>
        /// <returns>Base response indicating success or failure</returns>
        Task<BaseResponse<bool>> MarkNotificationAsReadAsync(int userId, int userNotificationId);

        /// <summary>
        /// Marks multiple notifications as read for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="userNotificationIds">List of user notification IDs to mark as read</param>
        /// <returns>Base response with count of notifications marked as read</returns>
        Task<BaseResponse<int>> MarkMultipleNotificationsAsReadAsync(int userId, List<int> userNotificationIds);

        /// <summary>
        /// Marks all unread notifications as read for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Base response with count of notifications marked as read</returns>
        Task<BaseResponse<int>> MarkAllNotificationsAsReadAsync(int userId);

        /// <summary>
        /// Gets the count of unread notifications for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Base response with unread notifications count</returns>
        Task<BaseResponse<int>> GetUnreadCountAsync(int userId);
    }
}