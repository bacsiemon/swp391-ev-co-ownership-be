using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.Models;

namespace EvCoOwnership.Services.Interfaces
{
    public interface IUserService
    {
        Task<BaseResponse> GetPagingAsync(int pageIndex, int pageSize);
    }
}