using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;

namespace EvCoOwnership.Repositories.Interfaces
{
    public interface ICoOwnerRepository : IGenericRepository<CoOwner>
    {
        /// <summary>
        /// Gets co-owner by user ID
        /// </summary>
        Task<CoOwner?> GetByUserIdAsync(int userId);
    }
}