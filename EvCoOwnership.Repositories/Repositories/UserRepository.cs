using EvCoOwnership.Repositories.Context;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvCoOwnership.Repositories.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(EvCoOwnershipDbContext context) : base(context)
        {

        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByNormalizedEmailAsync(string normalizedEmail)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            var normalizedEmail = email.ToUpperInvariant();
            return await _context.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail);
        }

        public async Task<User?> GetUserWithRolesAsync(int userId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetUserWithRolesByIdAsync(int userId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetUserWithRolesByEmailAsync(string email)
        {
            var normalizedEmail = email.ToUpperInvariant();
            return await _context.Users
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
        }
    }
}
