using EvCoOwnership.Repositories.Context;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;

namespace EvCoOwnership.Repositories.Repositories
{
    public class CoOwnerGroupRepository : GenericRepository<CoOwnerGroup>, ICoOwnerGroupRepository
    {
        public CoOwnerGroupRepository(EvCoOwnershipDbContext context) : base(context)
        {
        }
    }
}