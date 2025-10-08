using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;

namespace EvCoOwnership.Repositories.Interfaces
{
    public interface IDrivingLicenseRepository : IGenericRepository<DrivingLicense>
    {
        Task<DrivingLicense?> GetByLicenseNumberAsync(string licenseNumber);
        Task<DrivingLicense?> GetByLicenseNumberWithCoOwnerAsync(string licenseNumber);
        Task<bool> LicenseNumberExistsAsync(string licenseNumber);
        Task<List<DrivingLicense>> GetByCoOwnerIdAsync(int coOwnerId);
        Task<List<DrivingLicense>> GetExpiringLicensesAsync(int daysThreshold = 30);
    }
}