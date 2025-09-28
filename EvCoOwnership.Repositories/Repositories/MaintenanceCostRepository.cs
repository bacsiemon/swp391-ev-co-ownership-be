using EvCoOwnership.Repositories.Context;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;

namespace EvCoOwnership.Repositories.Repositories
{
    public class MaintenanceCostRepository : GenericRepository<MaintenanceCost>, IMaintenanceCostRepository
    {
        public MaintenanceCostRepository(EvCoOwnershipDbContext context) : base(context)
        {
        }
    }
}