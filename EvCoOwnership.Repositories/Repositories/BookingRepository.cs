using EvCoOwnership.Repositories.Context;
using EvCoOwnership.Repositories.Enums;
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

        public async Task<List<Booking>> GetBookingsForCalendarAsync(
            DateTime startDate,
            DateTime endDate,
            int? coOwnerId = null,
            int? vehicleId = null,
            EBookingStatus? status = null)
        {
            var query = _context.Bookings
                .Include(b => b.Vehicle)
                .Include(b => b.CoOwner)
                    .ThenInclude(co => co.User)
                .AsQueryable();

            // Filter by date range - bookings that overlap with the requested range
            query = query.Where(b =>
                b.StartTime < endDate && b.EndTime > startDate);

            // Role-based filtering: If coOwnerId is provided, only show bookings for vehicles in that co-owner's groups
            if (coOwnerId.HasValue)
            {
                query = query.Where(b => b.Vehicle.VehicleCoOwners.Any(vco =>
                    vco.CoOwnerId == coOwnerId.Value &&
                    vco.StatusEnum == EEContractStatus.Active));
            }

            // Filter by specific vehicle
            if (vehicleId.HasValue)
            {
                query = query.Where(b => b.VehicleId == vehicleId.Value);
            }

            // Filter by status
            if (status.HasValue)
            {
                query = query.Where(b => b.StatusEnum == status.Value);
            }

            // Order by start time for calendar view
            return await query
                .OrderBy(b => b.StartTime)
                .ToListAsync();
        }

        public async Task<List<Booking>> GetConflictingBookingsAsync(
            int vehicleId,
            DateTime startTime,
            DateTime endTime,
            int? excludeBookingId = null)
        {
            var query = _context.Bookings
                .Include(b => b.CoOwner)
                    .ThenInclude(co => co.User)
                .Where(b => b.VehicleId == vehicleId)
                // Only check non-cancelled bookings
                .Where(b => b.StatusEnum != EBookingStatus.Cancelled)
                // Check for time overlap: existing booking overlaps if:
                // (existing.start < new.end) AND (existing.end > new.start)
                .Where(b => b.StartTime < endTime && b.EndTime > startTime);

            // Exclude specific booking (useful when updating a booking)
            if (excludeBookingId.HasValue)
            {
                query = query.Where(b => b.Id != excludeBookingId.Value);
            }

            return await query.ToListAsync();
        }
    }
}