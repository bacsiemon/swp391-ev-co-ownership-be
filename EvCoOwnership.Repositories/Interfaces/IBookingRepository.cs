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
    }
}