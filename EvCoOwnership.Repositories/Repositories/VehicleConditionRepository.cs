using EvCoOwnership.Repositories.Context;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;

namespace EvCoOwnership.Repositories.Repositories
{
    public class VehicleConditionRepository : GenericRepository<VehicleCondition>, IVehicleConditionRepository
    {
        public VehicleConditionRepository(EvCoOwnershipDbContext context) : base(context)
        {
        }
    }
}