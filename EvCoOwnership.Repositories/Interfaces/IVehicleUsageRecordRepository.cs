using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;

namespace EvCoOwnership.Repositories.Interfaces
{
    public interface IVehicleUsageRecordRepository : IGenericRepository<VehicleUsageRecord>
    {
        /// <summary>
        /// Get all usage records for a specific vehicle
        /// </summary>
        Task<List<VehicleUsageRecord>> GetByVehicleIdAsync(int vehicleId);

        /// <summary>
        /// Get all usage records for a specific co-owner
        /// </summary>
        Task<List<VehicleUsageRecord>> GetByCoOwnerIdAsync(int coOwnerId);

        /// <summary>
        /// Get all usage records for a specific booking
        /// </summary>
        Task<VehicleUsageRecord?> GetByBookingIdAsync(int bookingId);

        /// <summary>
        /// Get usage records within a date range for a vehicle
        /// </summary>
        Task<List<VehicleUsageRecord>> GetByVehicleAndDateRangeAsync(
            int vehicleId,
            DateTime startDate,
            DateTime endDate);

        /// <summary>
        /// Get usage records within a date range for a co-owner
        /// </summary>
        Task<List<VehicleUsageRecord>> GetByCoOwnerAndDateRangeAsync(
            int coOwnerId,
            DateTime startDate,
            DateTime endDate);

        /// <summary>
        /// Get total distance traveled by vehicle
        /// </summary>
        Task<int> GetTotalDistanceByVehicleAsync(int vehicleId, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Get total hours used by vehicle
        /// </summary>
        Task<decimal> GetTotalHoursByVehicleAsync(int vehicleId, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Get total distance traveled by co-owner
        /// </summary>
        Task<int> GetTotalDistanceByCoOwnerAsync(int coOwnerId, int vehicleId, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Get total hours used by co-owner
        /// </summary>
        Task<decimal> GetTotalHoursByCoOwnerAsync(int coOwnerId, int vehicleId, DateTime? startDate = null, DateTime? endDate = null);
    }
}
