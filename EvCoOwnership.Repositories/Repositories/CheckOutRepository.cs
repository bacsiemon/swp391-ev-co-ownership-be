using EvCoOwnership.Repositories.Context;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;

namespace EvCoOwnership.Repositories.Repositories
{
    public class CheckOutRepository : GenericRepository<CheckOut>, ICheckOutRepository
    {
        public CheckOutRepository(EvCoOwnershipDbContext context) : base(context)
        {
        }
    }
}