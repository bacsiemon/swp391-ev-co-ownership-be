using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;
using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Repositories.Interfaces
{
    public interface IVehicleRepository : IGenericRepository<Vehicle>
    {
        Task<Vehicle?> GetVehicleWithVerificationHistoryAsync(int vehicleId);
        Task<IEnumerable<Vehicle>> GetVehiclesPendingVerificationAsync();
        Task<IEnumerable<Vehicle>> GetVehiclesByVerificationStatusAsync(EVehicleVerificationStatus status);
        Task<bool> IsVinUniqueAsync(string vin, int? excludeVehicleId = null);
        Task<bool> IsLicensePlateUniqueAsync(string licensePlate, int? excludeVehicleId = null);
        Task<IEnumerable<Vehicle>> GetVehiclesByFundIdAsync(int fundId);
    }
}