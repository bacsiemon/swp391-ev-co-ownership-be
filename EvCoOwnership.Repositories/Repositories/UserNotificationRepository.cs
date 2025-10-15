using EvCoOwnership.Repositories.Context;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace EvCoOwnership.Repositories.Repositories
{
    /// <summary>
    /// Repository implementation for UserNotification operations
    /// </summary>
    public class UserNotificationRepository : GenericRepository<UserNotification>, IUserNotificationRepository
    {
        public UserNotificationRepository(EvCoOwnershipDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Gets a user notification by ID
        /// </summary>
        /// <param name="id">User notification ID</param>
        /// <returns>User notification entity or null if not found</returns>
        public async Task<UserNotification?> GetByIdAsync(int id)
        {
            return await _context.UserNotifications
                .Include(un => un.Notification)
                .Include(un => un.User)
                .FirstOrDefaultAsync(un => un.Id == id);
        }

        /// <summary>
        /// Gets all notifications for a specific user with pagination
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="skip">Number of items to skip</param>
        /// <param name="take">Number of items to take</param>
        /// <param name="includeRead">Include read notifications</param>
        /// <returns>List of user notification entities with notification details</returns>
        public async Task<(IEnumerable<UserNotification> Items, int TotalCount)> GetByUserIdAsync(int userId, int skip = 0, int take = 10, bool includeRead = true)
        {
            var query = _context.UserNotifications
                .Include(un => un.Notification)
                .Where(un => un.UserId == userId);

            if (!includeRead)
            {
                query = query.Where(un => un.ReadAt == null);
            }

            var totalCount = await query.CountAsync();
            
            var items = await query
                .OrderByDescending(un => un.Notification!.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return (items, totalCount);
        }

        /// <summary>
        /// Creates a new user notification
        /// </summary>
        /// <param name="userNotification">User notification entity to create</param>
        /// <returns>Created user notification entity</returns>
        public async Task<UserNotification> CreateAsync(UserNotification userNotification)
        {
            _context.UserNotifications.Add(userNotification);
            await _context.SaveChangesAsync();
            return userNotification;
        }

        /// <summary>
        /// Creates multiple user notifications for a single notification
        /// </summary>
        /// <param name="notificationId">Notification ID</param>
        /// <param name="userIds">List of user IDs</param>
        /// <returns>List of created user notification entities</returns>
        public async Task<IEnumerable<UserNotification>> CreateBulkAsync(int notificationId, IEnumerable<int> userIds)
        {
            var userNotifications = userIds.Select(userId => new UserNotification
            {
                NotificationId = notificationId,
                UserId = userId
            }).ToList();

            _context.UserNotifications.AddRange(userNotifications);
            await _context.SaveChangesAsync();
            return userNotifications;
        }

        /// <summary>
        /// Marks a user notification as read
        /// </summary>
        /// <param name="userNotificationId">User notification ID</param>
        /// <returns>True if marked as read, false if not found</returns>
        public async Task<bool> MarkAsReadAsync(int userNotificationId)
        {
            var userNotification = await _context.UserNotifications.FindAsync(userNotificationId);
            if (userNotification == null)
                return false;

            userNotification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Marks multiple user notifications as read by their IDs
        /// </summary>
        /// <param name="userNotificationIds">List of user notification IDs</param>
        /// <returns>Number of notifications marked as read</returns>
        public async Task<int> MarkMultipleAsReadAsync(IEnumerable<int> userNotificationIds)
        {
            var userNotifications = await _context.UserNotifications
                .Where(un => userNotificationIds.Contains(un.Id) && un.ReadAt == null)
                .ToListAsync();

            foreach (var userNotification in userNotifications)
            {
                userNotification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return userNotifications.Count;
        }

        /// <summary>
        /// Marks all unread notifications for a user as read
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Number of notifications marked as read</returns>
        public async Task<int> MarkAllAsReadForUserAsync(int userId)
        {
            var userNotifications = await _context.UserNotifications
                .Where(un => un.UserId == userId && un.ReadAt == null)
                .ToListAsync();

            foreach (var userNotification in userNotifications)
            {
                userNotification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return userNotifications.Count;
        }

        /// <summary>
        /// Gets the count of unread notifications for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Count of unread notifications</returns>
        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.UserNotifications
                .Where(un => un.UserId == userId && un.ReadAt == null)
                .CountAsync();
        }

        /// <summary>
        /// Gets user notifications by notification IDs for a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="notificationIds">List of notification IDs</param>
        /// <returns>List of user notification entities</returns>
        public async Task<IEnumerable<UserNotification>> GetByNotificationIdsForUserAsync(int userId, IEnumerable<int> notificationIds)
        {
            return await _context.UserNotifications
                .Include(un => un.Notification)
                .Where(un => un.UserId == userId && notificationIds.Contains(un.NotificationId!.Value))
                .ToListAsync();
        }
    }
}