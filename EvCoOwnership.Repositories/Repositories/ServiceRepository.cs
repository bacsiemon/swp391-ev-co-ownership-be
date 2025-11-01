using System.Collections.Generic;
using System.Threading.Tasks;
using EvCoOwnership.Repositories.DTOs.ServiceDTOs;
using EvCoOwnership.Repositories.Interfaces;

namespace EvCoOwnership.Repositories.Repositories
{
    public class ServiceRepository : IServiceRepository
    {
        public Task<IEnumerable<ServiceDto>> ListAsync(object query)
        {
            return Task.FromResult<IEnumerable<ServiceDto>>(new List<ServiceDto>());
        }

        public Task<bool> StartAsync(int id)
        {
            return Task.FromResult(false);
        }

        public Task<bool> CompleteAsync(int id)
        {
            return Task.FromResult(false);
        }
    }
}
