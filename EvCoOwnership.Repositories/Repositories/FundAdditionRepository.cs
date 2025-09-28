using EvCoOwnership.Repositories.Context;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;

namespace EvCoOwnership.Repositories.Repositories
{
    public class FundAdditionRepository : GenericRepository<FundAddition>, IFundAdditionRepository
    {
        public FundAdditionRepository(EvCoOwnershipDbContext context) : base(context)
        {
        }
    }
}