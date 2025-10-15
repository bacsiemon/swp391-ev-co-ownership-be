using EvCoOwnership.Repositories.Context;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;
using EvCoOwnership.Repositories.Enums;
using Microsoft.EntityFrameworkCore;

namespace EvCoOwnership.Repositories.Repositories
{
    public class VehicleRepository : GenericRepository<Vehicle>, IVehicleRepository
    {
        public VehicleRepository(EvCoOwnershipDbContext context) : base(context)
        {
        }

        public async Task<Vehicle?> GetVehicleWithVerificationHistoryAsync(int vehicleId)
        {
            return await _context.Vehicles
                .Include(v => v.VehicleVerificationHistories)
                    .ThenInclude(h => h.Staff)
                .FirstOrDefaultAsync(v => v.Id == vehicleId);
        }

        public async Task<IEnumerable<Vehicle>> GetVehiclesPendingVerificationAsync()
        {
            return await _context.Vehicles
                .Where(v => v.VerificationStatusEnum == EVehicleVerificationStatus.Pending ||
                           v.VerificationStatusEnum == EVehicleVerificationStatus.VerificationRequested ||
                           v.VerificationStatusEnum == EVehicleVerificationStatus.RequiresRecheck)
                .Include(v => v.CreatedByNavigation)
                .ToListAsync();
        }

        public async Task<IEnumerable<Vehicle>> GetVehiclesByVerificationStatusAsync(EVehicleVerificationStatus status)
        {
            return await _context.Vehicles
                .Where(v => v.VerificationStatusEnum == status)
                .Include(v => v.CreatedByNavigation)
                .Include(v => v.VehicleVerificationHistories)
                .ToListAsync();
        }

        public async Task<bool> IsVinUniqueAsync(string vin, int? excludeVehicleId = null)
        {
            var query = _context.Vehicles.Where(v => v.Vin == vin);

            if (excludeVehicleId.HasValue)
            {
                query = query.Where(v => v.Id != excludeVehicleId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<bool> IsLicensePlateUniqueAsync(string licensePlate, int? excludeVehicleId = null)
        {
            var query = _context.Vehicles.Where(v => v.LicensePlate == licensePlate);

            if (excludeVehicleId.HasValue)
            {
                query = query.Where(v => v.Id != excludeVehicleId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<IEnumerable<Vehicle>> GetVehiclesByFundIdAsync(int fundId)
        {
            return await _context.Vehicles
                .Where(v => v.FundId == fundId)
                .Include(v => v.CreatedByNavigation)
                .ToListAsync();
        }
    }
}