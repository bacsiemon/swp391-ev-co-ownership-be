using EvCoOwnership.Repositories.Context;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace EvCoOwnership.Repositories.Repositories
{
    public class DrivingLicenseRepository : GenericRepository<DrivingLicense>, IDrivingLicenseRepository
    {
        public DrivingLicenseRepository(EvCoOwnershipDbContext context) : base(context)
        {
        }

        public async Task<DrivingLicense?> GetByLicenseNumberAsync(string licenseNumber)
        {
            return await _context.DrivingLicenses
                .Include(dl => dl.VerifiedByUser)
                .FirstOrDefaultAsync(dl => dl.LicenseNumber == licenseNumber);
        }

        public async Task<DrivingLicense?> GetByLicenseNumberWithCoOwnerAsync(string licenseNumber)
        {
            return await _context.DrivingLicenses
                .Include(dl => dl.CoOwner)
                .ThenInclude(co => co.User)
                .Include(dl => dl.VerifiedByUser)
                .FirstOrDefaultAsync(dl => dl.LicenseNumber == licenseNumber);
        }

        public async Task<bool> LicenseNumberExistsAsync(string licenseNumber)
        {
            return await _context.DrivingLicenses
                .AnyAsync(dl => dl.LicenseNumber == licenseNumber);
        }

        public async Task<List<DrivingLicense>> GetByCoOwnerIdAsync(int coOwnerId)
        {
            return await _context.DrivingLicenses
                .Where(dl => dl.CoOwnerId == coOwnerId)
                .ToListAsync();
        }

        public async Task<List<DrivingLicense>> GetExpiringLicensesAsync(int daysThreshold = 30)
        {
            var thresholdDate = DateOnly.FromDateTime(DateTime.Now.AddDays(daysThreshold));
            var today = DateOnly.FromDateTime(DateTime.Now);

            return await _context.DrivingLicenses
                .Include(dl => dl.CoOwner)
                .ThenInclude(co => co.User)
                .Where(dl => dl.ExpiryDate.HasValue
                           && dl.ExpiryDate.Value <= thresholdDate
                           && dl.ExpiryDate.Value > today)
                .ToListAsync();
        }

        public async Task<DrivingLicense?> GetByUserIdAsync(int userId)
        {
            return await _context.DrivingLicenses
                .Include(dl => dl.CoOwner)
                .ThenInclude(co => co.User)
                .Include(dl => dl.VerifiedByUser)
                .FirstOrDefaultAsync(dl => dl.CoOwner.UserId == userId);
        }

        /// <summary>
        /// Get license by ID with all related entities for admin operations
        /// </summary>
        public async Task<DrivingLicense?> GetByIdWithDetailsAsync(int licenseId)
        {
            return await _context.DrivingLicenses
                .Include(dl => dl.CoOwner)
                .ThenInclude(co => co.User)
                .Include(dl => dl.VerifiedByUser)
                .FirstOrDefaultAsync(dl => dl.Id == licenseId);
        }
    }
}