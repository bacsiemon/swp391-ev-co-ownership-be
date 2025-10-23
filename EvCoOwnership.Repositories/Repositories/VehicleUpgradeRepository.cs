using EvCoOwnership.Repositories.Context;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;

namespace EvCoOwnership.Repositories.Repositories
{
    public class VehicleUpgradeProposalRepository : GenericRepository<VehicleUpgradeProposal>, IVehicleUpgradeProposalRepository
    {
        public VehicleUpgradeProposalRepository(EvCoOwnershipDbContext context) : base(context)
        {
        }
    }

    public class VehicleUpgradeVoteRepository : GenericRepository<VehicleUpgradeVote>, IVehicleUpgradeVoteRepository
    {
        public VehicleUpgradeVoteRepository(EvCoOwnershipDbContext context) : base(context)
        {
        }
    }
}
