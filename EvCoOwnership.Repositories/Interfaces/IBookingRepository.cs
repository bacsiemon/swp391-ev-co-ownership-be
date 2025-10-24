using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;

namespace EvCoOwnership.Repositories.Interfaces
{
    public interface IBookingRepository : IGenericRepository<Booking>
    {
        /// <summary>
        /// Gets recent bookings for a user
        /// </summary>
        Task<List<Booking>> GetRecentBookingsByUserIdAsync(int userId, int count = 5);

        /// <summary>
        /// Gets total bookings count for a user
        /// </summary>
        Task<int> GetBookingsCountByUserIdAsync(int userId);

        /// <summary>
        /// Gets bookings within a date range for calendar view
        /// </summary>
        /// <param name="startDate">Start date of range</param>
        /// <param name="endDate">End date of range</param>
        /// <param name="coOwnerId">Optional: Filter by co-owner's vehicles only</param>
        /// <param name="vehicleId">Optional: Filter by specific vehicle</param>
        /// <param name="status">Optional: Filter by booking status</param>
        Task<List<Booking>> GetBookingsForCalendarAsync(
            DateTime startDate,
            DateTime endDate,
            int? coOwnerId = null,
            int? vehicleId = null,
            EBookingStatus? status = null);

        /// <summary>
        /// Checks if a vehicle has any conflicting bookings in the given time range
        /// </summary>
        Task<List<Booking>> GetConflictingBookingsAsync(
            int vehicleId,
            DateTime startTime,
            DateTime endTime,
            int? excludeBookingId = null);
    }
}