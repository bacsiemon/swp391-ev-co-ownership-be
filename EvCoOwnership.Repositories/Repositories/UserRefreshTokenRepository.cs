using EvCoOwnership.Repositories.Context;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace EvCoOwnership.Repositories.Repositories
{
    public class UserRefreshTokenRepository : GenericRepository<UserRefreshToken>, IUserRefreshTokenRepository
    {
        public UserRefreshTokenRepository(EvCoOwnershipDbContext context) : base(context)
        {
        }

        public async Task<UserRefreshToken?> GetByUserIdAsync(int userId)
        {
            return await _context.UserRefreshTokens
                .FirstOrDefaultAsync(urt => urt.UserId == userId);
        }

        public async Task<UserRefreshToken?> GetByRefreshTokenAsync(string refreshToken)
        {
            return await _context.UserRefreshTokens
                .Include(urt => urt.User)
                .FirstOrDefaultAsync(urt => urt.RefreshToken == refreshToken);
        }

        public async Task DeleteExpiredTokensAsync()
        {
            var expiredTokens = await _context.UserRefreshTokens
                .Where(urt => urt.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            if (expiredTokens.Any())
            {
                _context.UserRefreshTokens.RemoveRange(expiredTokens);
            }
        }
    }
}