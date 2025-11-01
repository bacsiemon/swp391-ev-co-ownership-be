using System.Collections.Generic;
using System.Threading.Tasks;
using EvCoOwnership.Repositories.DTOs;
using EvCoOwnership.Repositories.DTOs.GroupDTOs;

namespace EvCoOwnership.Services.Interfaces
{
    public interface IGroupService
    {
        Task<IEnumerable<GroupDto>> ListAsync(object query);
        Task<GroupDto> GetAsync(int id);
        Task<GroupDto> CreateAsync(CreateGroupDto dto);
        Task<GroupDto> UpdateAsync(int id, CreateGroupDto dto);
        Task<bool> RemoveAsync(int id);
        // Members, Votes, Fund methods omitted for brevity
    }
}