using System.Collections.Generic;
using System.Threading.Tasks;
using EvCoOwnership.Repositories.DTOs;
using EvCoOwnership.Repositories.DTOs.ServiceDTOs;
using EvCoOwnership.Services.Interfaces;

namespace EvCoOwnership.Services.Services
{
    public class ServiceService : IServiceService
    {
        public Task<IEnumerable<ServiceDto>> ListAsync(object query) => Task.FromResult<IEnumerable<ServiceDto>>(new List<ServiceDto>());
        public Task<bool> StartAsync(int id) => Task.FromResult(false);
        public Task<bool> CompleteAsync(int id) => Task.FromResult(false);
    }
}