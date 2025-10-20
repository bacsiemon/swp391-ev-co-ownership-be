using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;

namespace EvCoOwnership.Repositories.Interfaces
{
    public interface IVehicleCoOwnerRepository : IGenericRepository<VehicleCoOwner>
    {
        /// <summary>
        /// Gets pending invitations for a specific co-owner
        /// </summary>
        Task<List<VehicleCoOwner>> GetPendingInvitationsByCoOwnerAsync(int coOwnerId);

        /// <summary>
        /// Gets all vehicle co-owner relationships for a specific vehicle
        /// </summary>
        Task<List<VehicleCoOwner>> GetByVehicleIdAsync(int vehicleId);

        /// <summary>
        /// Gets vehicle co-owner relationship by vehicle and co-owner
        /// </summary>
        Task<VehicleCoOwner?> GetByVehicleAndCoOwnerAsync(int vehicleId, int coOwnerId);
    }
}