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
            EVehicleVerificationStatus? verificationStatusFilter = null,
            string? brandFilter = null,
            string? modelFilter = null,
            int? minYear = null,
            int? maxYear = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? searchKeyword = null,
            string? sortBy = null,
            bool sortDescending = true)
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

            // Brand filter
            if (!string.IsNullOrWhiteSpace(brandFilter))
            {
                query = query.Where(v => v.Brand.ToLower().Contains(brandFilter.ToLower()));
            }

            // Model filter
            if (!string.IsNullOrWhiteSpace(modelFilter))
            {
                query = query.Where(v => v.Model.ToLower().Contains(modelFilter.ToLower()));
            }

            // Year range filter
            if (minYear.HasValue)
            {
                query = query.Where(v => v.Year >= minYear.Value);
            }
            if (maxYear.HasValue)
            {
                query = query.Where(v => v.Year <= maxYear.Value);
            }

            // Price range filter
            if (minPrice.HasValue)
            {
                query = query.Where(v => v.PurchasePrice >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(v => v.PurchasePrice <= maxPrice.Value);
            }

            // Search keyword (searches in name, brand, model, VIN, license plate)
            if (!string.IsNullOrWhiteSpace(searchKeyword))
            {
                var keyword = searchKeyword.ToLower();
                query = query.Where(v =>
                    v.Name.ToLower().Contains(keyword) ||
                    v.Brand.ToLower().Contains(keyword) ||
                    v.Model.ToLower().Contains(keyword) ||
                    v.Vin.ToLower().Contains(keyword) ||
                    v.LicensePlate.ToLower().Contains(keyword));
            }

            // Sorting
            query = sortBy?.ToLower() switch
            {
                "name" => sortDescending ? query.OrderByDescending(v => v.Name) : query.OrderBy(v => v.Name),
                "brand" => sortDescending ? query.OrderByDescending(v => v.Brand) : query.OrderBy(v => v.Brand),
                "model" => sortDescending ? query.OrderByDescending(v => v.Model) : query.OrderBy(v => v.Model),
                "year" => sortDescending ? query.OrderByDescending(v => v.Year) : query.OrderBy(v => v.Year),
                "price" => sortDescending ? query.OrderByDescending(v => v.PurchasePrice) : query.OrderBy(v => v.PurchasePrice),
                "createdat" => sortDescending ? query.OrderByDescending(v => v.CreatedAt) : query.OrderBy(v => v.CreatedAt),
                _ => query.OrderByDescending(v => v.CreatedAt) // Default: newest first
            };

            var totalCount = await query.CountAsync();

            var vehicles = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (vehicles, totalCount);
        }
    }
}