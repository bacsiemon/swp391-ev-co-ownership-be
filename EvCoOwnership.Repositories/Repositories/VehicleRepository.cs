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

        /// <summary>
        /// Gets vehicle with co-owners included
        /// </summary>
        public async Task<Vehicle?> GetVehicleWithCoOwnersAsync(int vehicleId)
        {
            return await _context.Set<Vehicle>()
                .Include(v => v.VehicleCoOwners)
                .ThenInclude(vco => vco.CoOwner)
                .ThenInclude(co => co.User)
                .FirstOrDefaultAsync(v => v.Id == vehicleId);
        }

        /// <summary>
        /// Gets vehicles by co-owner ID
        /// </summary>
        public async Task<List<Vehicle>> GetVehiclesByCoOwnerAsync(int coOwnerId)
        {
            return await _context.Set<Vehicle>()
                .Include(v => v.VehicleCoOwners)
                .ThenInclude(vco => vco.CoOwner)
                .ThenInclude(co => co.User)
                .Where(v => v.VehicleCoOwners.Any(vco => vco.CoOwnerId == coOwnerId))
                .ToListAsync();
        }

        /// <summary>
        /// Gets vehicle by license plate
        /// </summary>
        public async Task<Vehicle?> GetByLicensePlateAsync(string licensePlate)
        {
            return await _context.Set<Vehicle>()
                .FirstOrDefaultAsync(v => v.LicensePlate == licensePlate);
        }

        /// <summary>
        /// Gets vehicle by VIN
        /// </summary>
        public async Task<Vehicle?> GetByVinAsync(string vin)
        {
            return await _context.Set<Vehicle>()
                .FirstOrDefaultAsync(v => v.Vin == vin);
        }
    }
}