using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;

namespace EvCoOwnership.Repositories.Interfaces
{
    public interface IUserRefreshTokenRepository : IGenericRepository<UserRefreshToken>
    {
        Task<UserRefreshToken?> GetByUserIdAsync(int userId);
        Task<UserRefreshToken?> GetByRefreshTokenAsync(string refreshToken);
        Task DeleteExpiredTokensAsync();
    }
}