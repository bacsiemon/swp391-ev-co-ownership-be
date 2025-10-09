using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;
using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Repositories.Interfaces
{
    public interface IRoleRepository : IGenericRepository<Role>
    {
        Task<Role?> GetByRoleNameAsync(EUserRole roleName);
        Task<List<Role>> GetRolesByUserIdAsync(int userId);
    }
}