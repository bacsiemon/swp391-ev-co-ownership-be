using EvCoOwnership.Repositories.Models;

namespace EvCoOwnership.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for NotificationEntity operations
    /// </summary>
    public interface INotificationRepository
    {
        /// <summary>
        /// Gets a notification by ID
        /// </summary>
        /// <param name="id">Notification ID</param>
        /// <returns>Notification entity or null if not found</returns>
        Task<NotificationEntity?> GetByIdAsync(int id);

        /// <summary>
        /// Creates a new notification
        /// </summary>
        /// <param name="notification">Notification entity to create</param>
        /// <returns>Created notification entity</returns>
        Task<NotificationEntity> CreateAsync(NotificationEntity notification);

        /// <summary>
        /// Gets all notifications with optional filtering
        /// </summary>
        /// <param name="skip">Number of items to skip</param>
        /// <param name="take">Number of items to take</param>
        /// <returns>List of notification entities</returns>
        Task<IEnumerable<NotificationEntity>> GetAllAsync(int skip = 0, int take = int.MaxValue);

        /// <summary>
        /// Deletes a notification by ID
        /// </summary>
        /// <param name="id">Notification ID to delete</param>
        /// <returns>True if deleted, false if not found</returns>
        Task<bool> DeleteAsync(int id);
    }
}