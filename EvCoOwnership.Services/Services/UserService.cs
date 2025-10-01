using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvCoOwnership.Services.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponse> GetPagingAsync(int pageIndex, int pageSize)
        {
            var users = await _unitOfWork.UserRepository.GetPaginatedAsync(pageIndex, pageSize, 1, e => e.OrderBy(e => e.Id));

            return new BaseResponse
            {
                StatusCode = 200,
                Message = "SUCCESS",
                Data = users.Items,
                AdditionalData = users.AdditionalData
            };
        }
    }
}
