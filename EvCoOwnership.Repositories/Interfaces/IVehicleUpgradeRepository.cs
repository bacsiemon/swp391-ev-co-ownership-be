using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;

namespace EvCoOwnership.Repositories.Interfaces
{
    public interface IVehicleUpgradeProposalRepository : IGenericRepository<VehicleUpgradeProposal>
    {
    }

    public interface IVehicleUpgradeVoteRepository : IGenericRepository<VehicleUpgradeVote>
    {
    }
}
