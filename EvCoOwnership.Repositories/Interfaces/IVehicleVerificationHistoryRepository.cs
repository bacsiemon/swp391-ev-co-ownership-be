using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;
using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Repositories.Interfaces
{
    public interface IVehicleVerificationHistoryRepository : IGenericRepository<VehicleVerificationHistory>
    {
        Task<IEnumerable<VehicleVerificationHistory>> GetVerificationHistoryByVehicleIdAsync(int vehicleId);
        Task<VehicleVerificationHistory?> GetLatestVerificationByVehicleIdAsync(int vehicleId);
        Task<IEnumerable<VehicleVerificationHistory>> GetVerificationHistoryByStaffIdAsync(int staffId);
    }
}