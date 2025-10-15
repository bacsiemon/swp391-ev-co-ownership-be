using EvCoOwnership.Repositories.Models;

namespace EvCoOwnership.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for UserNotification operations
    /// </summary>
    public interface IUserNotificationRepository
    {
        /// <summary>
        /// Gets a user notification by ID
        /// </summary>
        /// <param name="id">User notification ID</param>
        /// <returns>User notification entity or null if not found</returns>
        Task<UserNotification?> GetByIdAsync(int id);

        /// <summary>
        /// Gets all notifications for a specific user with pagination
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="skip">Number of items to skip</param>
        /// <param name="take">Number of items to take</param>
        /// <param name="includeRead">Include read notifications</param>
        /// <returns>List of user notification entities with notification details</returns>
        Task<(IEnumerable<UserNotification> Items, int TotalCount)> GetByUserIdAsync(int userId, int skip = 0, int take = 10, bool includeRead = true);

        /// <summary>
        /// Creates a new user notification
        /// </summary>
        /// <param name="userNotification">User notification entity to create</param>
        /// <returns>Created user notification entity</returns>
        Task<UserNotification> CreateAsync(UserNotification userNotification);

        /// <summary>
        /// Creates multiple user notifications for a single notification
        /// </summary>
        /// <param name="notificationId">Notification ID</param>
        /// <param name="userIds">List of user IDs</param>
        /// <returns>List of created user notification entities</returns>
        Task<IEnumerable<UserNotification>> CreateBulkAsync(int notificationId, IEnumerable<int> userIds);

        /// <summary>
        /// Marks a user notification as read
        /// </summary>
        /// <param name="userNotificationId">User notification ID</param>
        /// <returns>True if marked as read, false if not found</returns>
        Task<bool> MarkAsReadAsync(int userNotificationId);

        /// <summary>
        /// Marks multiple user notifications as read by their IDs
        /// </summary>
        /// <param name="userNotificationIds">List of user notification IDs</param>
        /// <returns>Number of notifications marked as read</returns>
        Task<int> MarkMultipleAsReadAsync(IEnumerable<int> userNotificationIds);

        /// <summary>
        /// Marks all unread notifications for a user as read
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Number of notifications marked as read</returns>
        Task<int> MarkAllAsReadForUserAsync(int userId);

        /// <summary>
        /// Gets the count of unread notifications for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Count of unread notifications</returns>
        Task<int> GetUnreadCountAsync(int userId);

        /// <summary>
        /// Gets user notifications by notification IDs for a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="notificationIds">List of notification IDs</param>
        /// <returns>List of user notification entities</returns>
        Task<IEnumerable<UserNotification>> GetByNotificationIdsForUserAsync(int userId, IEnumerable<int> notificationIds);
    }
}