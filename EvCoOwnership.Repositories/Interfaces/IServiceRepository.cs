using System.Collections.Generic;
using System.Threading.Tasks;
using EvCoOwnership.Repositories.DTOs;
using EvCoOwnership.Repositories.DTOs.ServiceDTOs;

namespace EvCoOwnership.Repositories.Interfaces
{
    public interface IServiceRepository
    {
        Task<IEnumerable<ServiceDto>> ListAsync(object query);
        Task<bool> StartAsync(int id);
        Task<bool> CompleteAsync(int id);
    }
}