using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.Models;

namespace EvCoOwnership.Services.Interfaces
{
    public interface IUserService
    {
        Task<PaginatedList<User>> GetUsersAsync(int pageIndex, int pageSize);
    }
}