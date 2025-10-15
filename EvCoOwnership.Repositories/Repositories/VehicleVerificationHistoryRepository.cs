using EvCoOwnership.Repositories.Context;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace EvCoOwnership.Repositories.Repositories
{
    public class VehicleVerificationHistoryRepository : GenericRepository<VehicleVerificationHistory>, IVehicleVerificationHistoryRepository
    {
        public VehicleVerificationHistoryRepository(EvCoOwnershipDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<VehicleVerificationHistory>> GetVerificationHistoryByVehicleIdAsync(int vehicleId)
        {
            return await _context.VehicleVerificationHistories
                .Where(h => h.VehicleId == vehicleId)
                .Include(h => h.Staff)
                .Include(h => h.Vehicle)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();
        }

        public async Task<VehicleVerificationHistory?> GetLatestVerificationByVehicleIdAsync(int vehicleId)
        {
            return await _context.VehicleVerificationHistories
                .Where(h => h.VehicleId == vehicleId)
                .Include(h => h.Staff)
                .Include(h => h.Vehicle)
                .OrderByDescending(h => h.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<VehicleVerificationHistory>> GetVerificationHistoryByStaffIdAsync(int staffId)
        {
            return await _context.VehicleVerificationHistories
                .Where(h => h.StaffId == staffId)
                .Include(h => h.Vehicle)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();
        }
    }
}