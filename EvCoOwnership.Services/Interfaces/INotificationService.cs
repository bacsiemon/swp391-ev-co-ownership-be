using EvCoOwnership.Repositories.DTOs.NotificationDTOs;
using EvCoOwnership.Helpers.BaseClasses;

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
        /// <param name="request">Create notification request containing notification details</param>
        /// <returns>Base response indicating success or failure</returns>
        Task<BaseResponse<int>> SendNotificationToUsersAsync(CreateNotificationRequest request);

        /// <summary>
        /// Sends a notification to a single user and fires notification event
        /// </summary>
        /// <param name="request">Send notification request containing user and notification details</param>
        /// <returns>Base response indicating success or failure</returns>
        Task<BaseResponse<int>> SendNotificationToUserAsync(SendNotificationRequestDto request);

        /// <summary>
        /// Gets paginated notifications for a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="pageIndex">Page index (starts from 1)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="includeRead">Include read notifications</param>
        /// <returns>Paginated list of user notification response DTOs</returns>
        Task<BaseResponse<PaginatedList<UserNotificationResponseDto>>> GetUserNotificationsAsync(int userId, int pageIndex = 1, int pageSize = 10, bool includeRead = true);

        /// <summary>
        /// Marks a single notification as read for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="request">Mark notification as read request containing notification ID</param>
        /// <returns>Base response indicating success or failure</returns>
        Task<BaseResponse<bool>> MarkNotificationAsReadAsync(int userId, MarkNotificationAsReadRequest request);

        /// <summary>
        /// Marks multiple notifications as read for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="request">Mark multiple notifications as read request containing notification IDs</param>
        /// <returns>Base response with count of notifications marked as read</returns>
        Task<BaseResponse<int>> MarkMultipleNotificationsAsReadAsync(int userId, MarkMultipleNotificationsAsReadRequest request);

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