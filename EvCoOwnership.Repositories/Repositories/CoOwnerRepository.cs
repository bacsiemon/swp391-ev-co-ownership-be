using EvCoOwnership.Repositories.Context;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;

namespace EvCoOwnership.Repositories.Repositories
{
    public class CoOwnerRepository : GenericRepository<CoOwner>, ICoOwnerRepository
    {
        public CoOwnerRepository(EvCoOwnershipDbContext context) : base(context)
        {
        }
    }
}