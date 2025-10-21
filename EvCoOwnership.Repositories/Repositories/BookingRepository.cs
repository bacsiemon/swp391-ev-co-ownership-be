using EvCoOwnership.Repositories.Context;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace EvCoOwnership.Repositories.Repositories
{
    public class BookingRepository : GenericRepository<Booking>, IBookingRepository
    {
        public BookingRepository(EvCoOwnershipDbContext context) : base(context)
        {
        }

        public async Task<List<Booking>> GetRecentBookingsByUserIdAsync(int userId, int count = 5)
        {
            return await _context.Bookings
                .Include(b => b.Vehicle)
                .Include(b => b.CoOwner)
                .Where(b => b.CoOwner.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<int> GetBookingsCountByUserIdAsync(int userId)
        {
            return await _context.Bookings
                .Include(b => b.CoOwner)
                .Where(b => b.CoOwner.UserId == userId)
                .CountAsync();
        }
    }
}