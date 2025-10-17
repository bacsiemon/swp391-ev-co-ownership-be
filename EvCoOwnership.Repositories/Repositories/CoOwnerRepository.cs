using EvCoOwnership.Repositories.Context;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace EvCoOwnership.Repositories.Repositories
{
    public class CoOwnerRepository : GenericRepository<CoOwner>, ICoOwnerRepository
    {
        public CoOwnerRepository(EvCoOwnershipDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Gets co-owner by user ID
        /// </summary>
        public async Task<CoOwner?> GetByUserIdAsync(int userId)
        {
            return await _context.Set<CoOwner>()
                .Include(co => co.User)
                .FirstOrDefaultAsync(co => co.UserId == userId);
        }
    }
}