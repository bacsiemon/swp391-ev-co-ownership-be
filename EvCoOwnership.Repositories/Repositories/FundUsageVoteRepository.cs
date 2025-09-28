using EvCoOwnership.Repositories.Context;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;

namespace EvCoOwnership.Repositories.Repositories
{
    public class FundUsageVoteRepository : GenericRepository<FundUsageVote>, IFundUsageVoteRepository
    {
        public FundUsageVoteRepository(EvCoOwnershipDbContext context) : base(context)
        {
        }
    }
}