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

        /// <summary>
        /// Gets all available vehicles with pagination and filters
        /// If coOwnerId is provided (Co-owner role): returns only vehicles in their groups
        /// If coOwnerId is null (Staff/Admin role): returns all vehicles
        /// </summary>
        public async Task<(List<Vehicle> vehicles, int totalCount)> GetAllAvailableVehiclesAsync(
            int pageIndex,
            int pageSize,
            int? coOwnerId = null,
            EVehicleStatus? statusFilter = null,
            EVehicleVerificationStatus? verificationStatusFilter = null)
        {
            var query = _context.Set<Vehicle>()
                .Include(v => v.VehicleCoOwners)
                .ThenInclude(vco => vco.CoOwner)
                .ThenInclude(co => co.User)
                .Include(v => v.CreatedByNavigation)
                .AsQueryable();

            // Role-based filtering:
            // If coOwnerId is provided (Co-owner): only show vehicles they are part of
            // If coOwnerId is null (Staff/Admin): show all vehicles
            if (coOwnerId.HasValue)
            {
                query = query.Where(v => v.VehicleCoOwners.Any(vco =>
                    vco.CoOwnerId == coOwnerId.Value &&
                    vco.StatusEnum == EContractStatus.Active));
            }

            // Apply status filter if provided
            if (statusFilter.HasValue)
            {
                query = query.Where(v => v.StatusEnum == statusFilter.Value);
            }
            else
            {
                // Default: only show available and verified vehicles
                query = query.Where(v => v.StatusEnum == EVehicleStatus.Available);
            }

            // Apply verification status filter if provided
            if (verificationStatusFilter.HasValue)
            {
                query = query.Where(v => v.VerificationStatusEnum == verificationStatusFilter.Value);
            }
            else
            {
                // Default: only show verified vehicles
                query = query.Where(v => v.VerificationStatusEnum == EVehicleVerificationStatus.Verified);
            }

            // Order by creation date (newest first)
            query = query.OrderByDescending(v => v.CreatedAt);

            var totalCount = await query.CountAsync();

            var vehicles = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (vehicles, totalCount);
        }
    }
}