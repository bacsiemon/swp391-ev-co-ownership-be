using EvCoOwnership.Repositories.Context;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace EvCoOwnership.Repositories.Repositories
{
    public class VehicleUsageRecordRepository : GenericRepository<VehicleUsageRecord>, IVehicleUsageRecordRepository
    {
        public VehicleUsageRecordRepository(EvCoOwnershipDbContext context) : base(context)
        {
        }

        public async Task<List<VehicleUsageRecord>> GetByVehicleIdAsync(int vehicleId)
        {
            return await _context.Set<VehicleUsageRecord>()
                .Where(r => r.VehicleId == vehicleId)
                .Include(r => r.CoOwner)
                    .ThenInclude(co => co.User)
                .Include(r => r.Vehicle)
                .Include(r => r.Booking)
                .OrderByDescending(r => r.StartTime)
                .ToListAsync();
        }

        public async Task<List<VehicleUsageRecord>> GetByCoOwnerIdAsync(int coOwnerId)
        {
            return await _context.Set<VehicleUsageRecord>()
                .Where(r => r.CoOwnerId == coOwnerId)
                .Include(r => r.Vehicle)
                .Include(r => r.Booking)
                .OrderByDescending(r => r.StartTime)
                .ToListAsync();
        }

        public async Task<VehicleUsageRecord?> GetByBookingIdAsync(int bookingId)
        {
            return await _context.Set<VehicleUsageRecord>()
                .Include(r => r.CoOwner)
                    .ThenInclude(co => co.User)
                .Include(r => r.Vehicle)
                .Include(r => r.CheckIn)
                .Include(r => r.CheckOut)
                .FirstOrDefaultAsync(r => r.BookingId == bookingId);
        }

        public async Task<List<VehicleUsageRecord>> GetByVehicleAndDateRangeAsync(
            int vehicleId,
            DateTime startDate,
            DateTime endDate)
        {
            return await _context.Set<VehicleUsageRecord>()
                .Where(r => r.VehicleId == vehicleId &&
                           r.StartTime >= startDate &&
                           r.EndTime <= endDate)
                .Include(r => r.CoOwner)
                    .ThenInclude(co => co.User)
                .Include(r => r.Vehicle)
                .OrderBy(r => r.StartTime)
                .ToListAsync();
        }

        public async Task<List<VehicleUsageRecord>> GetByCoOwnerAndDateRangeAsync(
            int coOwnerId,
            DateTime startDate,
            DateTime endDate)
        {
            return await _context.Set<VehicleUsageRecord>()
                .Where(r => r.CoOwnerId == coOwnerId &&
                           r.StartTime >= startDate &&
                           r.EndTime <= endDate)
                .Include(r => r.Vehicle)
                .Include(r => r.Booking)
                .OrderBy(r => r.StartTime)
                .ToListAsync();
        }

        public async Task<int> GetTotalDistanceByVehicleAsync(int vehicleId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Set<VehicleUsageRecord>()
                .Where(r => r.VehicleId == vehicleId && r.DistanceKm.HasValue);

            if (startDate.HasValue)
                query = query.Where(r => r.StartTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(r => r.EndTime <= endDate.Value);

            return await query.SumAsync(r => r.DistanceKm ?? 0);
        }

        public async Task<decimal> GetTotalHoursByVehicleAsync(int vehicleId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Set<VehicleUsageRecord>()
                .Where(r => r.VehicleId == vehicleId);

            if (startDate.HasValue)
                query = query.Where(r => r.StartTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(r => r.EndTime <= endDate.Value);

            return await query.SumAsync(r => r.DurationHours);
        }

        public async Task<int> GetTotalDistanceByCoOwnerAsync(int coOwnerId, int vehicleId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Set<VehicleUsageRecord>()
                .Where(r => r.CoOwnerId == coOwnerId &&
                           r.VehicleId == vehicleId &&
                           r.DistanceKm.HasValue);

            if (startDate.HasValue)
                query = query.Where(r => r.StartTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(r => r.EndTime <= endDate.Value);

            return await query.SumAsync(r => r.DistanceKm ?? 0);
        }

        public async Task<decimal> GetTotalHoursByCoOwnerAsync(int coOwnerId, int vehicleId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Set<VehicleUsageRecord>()
                .Where(r => r.CoOwnerId == coOwnerId && r.VehicleId == vehicleId);

            if (startDate.HasValue)
                query = query.Where(r => r.StartTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(r => r.EndTime <= endDate.Value);

            return await query.SumAsync(r => r.DurationHours);
        }
    }
}
