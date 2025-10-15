using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.Context;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;

namespace EvCoOwnership.Services
{
    public class UserService : IUserService
    {
        private readonly EvCoOwnershipDbContext _context;

        public UserService(EvCoOwnershipDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User> CreateAsync(User user)
        {
            user.CreatedAt = DateTime.UtcNow;
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User?> UpdateAsync(int id, User updatedUser)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return null;

            user.FirstName = updatedUser.FirstName;
            user.LastName = updatedUser.LastName;
            user.Phone = updatedUser.Phone;
            user.Address = updatedUser.Address;
            user.ProfileImageUrl = updatedUser.ProfileImageUrl;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<PagedResult<User>> GetPagingAsync(int pageIndex, int pageSize)
        {
            var totalCount = await _context.Users.CountAsync();

            var users = await _context.Users
                .OrderBy(u => u.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<User>(users, totalCount, pageIndex, pageSize);
        }
    }
}
