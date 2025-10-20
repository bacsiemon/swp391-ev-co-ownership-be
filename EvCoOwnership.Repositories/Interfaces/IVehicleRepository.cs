using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;

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
    }
}