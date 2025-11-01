using System.Collections.Generic;
using System.Threading.Tasks;
using EvCoOwnership.Repositories.DTOs;
using EvCoOwnership.Repositories.DTOs.ServiceDTOs;

namespace EvCoOwnership.Services.Interfaces
{
    public interface IServiceService
    {
        Task<IEnumerable<ServiceDto>> ListAsync(object query);
        Task<bool> StartAsync(int id);
        Task<bool> CompleteAsync(int id);
    }
}