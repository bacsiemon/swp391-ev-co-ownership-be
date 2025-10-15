using EvCoOwnership.Repositories.Context;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace EvCoOwnership.Repositories.Repositories
{
    /// <summary>
    /// Repository implementation for NotificationEntity operations
    /// </summary>
    public class NotificationRepository : GenericRepository<NotificationEntity>, INotificationRepository
    {
        public NotificationRepository(EvCoOwnershipDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Gets a notification by ID
        /// </summary>
        /// <param name="id">Notification ID</param>
        /// <returns>Notification entity or null if not found</returns>
        public async Task<NotificationEntity?> GetByIdAsync(int id)
        {
            return await _context.NotificationEntities
                .Include(n => n.UserNotifications)
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        /// <summary>
        /// Creates a new notification
        /// </summary>
        /// <param name="notification">Notification entity to create</param>
        /// <returns>Created notification entity</returns>
        public async Task<NotificationEntity> CreateAsync(NotificationEntity notification)
        {
            notification.CreatedAt = DateTime.UtcNow;
            _context.NotificationEntities.Add(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        /// <summary>
        /// Gets all notifications with optional filtering
        /// </summary>
        /// <param name="skip">Number of items to skip</param>
        /// <param name="take">Number of items to take</param>
        /// <returns>List of notification entities</returns>
        public async Task<IEnumerable<NotificationEntity>> GetAllAsync(int skip = 0, int take = int.MaxValue)
        {
            return await _context.NotificationEntities
                .Include(n => n.UserNotifications)
                .OrderByDescending(n => n.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        /// <summary>
        /// Deletes a notification by ID
        /// </summary>
        /// <param name="id">Notification ID to delete</param>
        /// <returns>True if deleted, false if not found</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            var notification = await _context.NotificationEntities.FindAsync(id);
            if (notification == null)
                return false;

            _context.NotificationEntities.Remove(notification);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}