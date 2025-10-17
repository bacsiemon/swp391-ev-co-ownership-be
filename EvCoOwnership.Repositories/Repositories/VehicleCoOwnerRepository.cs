using EvCoOwnership.Repositories.Context;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;
using EvCoOwnership.Repositories.Enums;
using Microsoft.EntityFrameworkCore;

namespace EvCoOwnership.Repositories.Repositories
{
    public class VehicleCoOwnerRepository : GenericRepository<VehicleCoOwner>, IVehicleCoOwnerRepository
    {
        public VehicleCoOwnerRepository(EvCoOwnershipDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Gets pending invitations for a specific co-owner
        /// </summary>
        public async Task<List<VehicleCoOwner>> GetPendingInvitationsByCoOwnerAsync(int coOwnerId)
        {
            return await _context.Set<VehicleCoOwner>()
                .Where(vco => vco.CoOwnerId == coOwnerId && vco.StatusEnum == EContractStatus.Pending)
                .Include(vco => vco.Vehicle)
                .ToListAsync();
        }

        /// <summary>
        /// Gets all vehicle co-owner relationships for a specific vehicle
        /// </summary>
        public async Task<List<VehicleCoOwner>> GetByVehicleIdAsync(int vehicleId)
        {
            return await _context.Set<VehicleCoOwner>()
                .Where(vco => vco.VehicleId == vehicleId)
                .Include(vco => vco.CoOwner)
                .ThenInclude(co => co.User)
                .ToListAsync();
        }

        /// <summary>
        /// Gets vehicle co-owner relationship by vehicle and co-owner
        /// </summary>
        public async Task<VehicleCoOwner?> GetByVehicleAndCoOwnerAsync(int vehicleId, int coOwnerId)
        {
            return await _context.Set<VehicleCoOwner>()
                .FirstOrDefaultAsync(vco => vco.VehicleId == vehicleId && vco.CoOwnerId == coOwnerId);
        }
    }
}