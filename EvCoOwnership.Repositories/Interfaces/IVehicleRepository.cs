using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;
using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Repositories.Interfaces
{
    public interface IVehicleRepository : IGenericRepository<Vehicle>
    {
        /// <summary>
        /// Gets vehicle with co-owners included
        /// </summary>
        Task<Vehicle?> GetVehicleWithCoOwnersAsync(int vehicleId);

        /// <summary>
        /// Gets vehicles by co-owner ID
        /// </summary>
        Task<List<Vehicle>> GetVehiclesByCoOwnerAsync(int coOwnerId);

        /// <summary>
        /// Gets vehicle by license plate
        /// </summary>
        Task<Vehicle?> GetByLicensePlateAsync(string licensePlate);

        /// <summary>
        /// Gets vehicle by VIN
        /// </summary>
        Task<Vehicle?> GetByVinAsync(string vin);

        /// <summary>
        /// Gets all available vehicles with pagination and optional filters
        /// For Co-owners: returns only vehicles in their groups
        /// For Staff/Admin: returns all vehicles
        /// </summary>
        Task<(List<Vehicle> vehicles, int totalCount)> GetAllAvailableVehiclesAsync(
            int pageIndex,
            int pageSize,
            int? coOwnerId = null,
            EVehicleStatus? statusFilter = null,
            EVehicleVerificationStatus? verificationStatusFilter = null);
    }
}