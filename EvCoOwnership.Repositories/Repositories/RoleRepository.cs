using EvCoOwnership.Repositories.Context;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;
using EvCoOwnership.Repositories.Enums;
using Microsoft.EntityFrameworkCore;

namespace EvCoOwnership.Repositories.Repositories
{
    public class RoleRepository : GenericRepository<Role>, IRoleRepository
    {
        public RoleRepository(EvCoOwnershipDbContext context) : base(context)
        {
        }

        public async Task<Role?> GetByRoleNameAsync(EUserRole roleName)
        {
            return await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleNameEnum == roleName);
        }

        public async Task<List<Role>> GetRolesByUserIdAsync(int userId)
        {
            return await _context.Roles
                .Where(r => r.Users.Any(u => u.Id == userId))
                .ToListAsync();
        }
    }
}