using System.Collections.Generic;
using System.Threading.Tasks;
using EvCoOwnership.Repositories.DTOs;
using EvCoOwnership.Repositories.DTOs.GroupDTOs;
using EvCoOwnership.Services.Interfaces;

namespace EvCoOwnership.Services.Services
{
    public class GroupService : IGroupService
    {
        public Task<IEnumerable<GroupDto>> ListAsync(object query) => Task.FromResult<IEnumerable<GroupDto>>(new List<GroupDto>());
        public Task<GroupDto> GetAsync(int id) => Task.FromResult<GroupDto>(null);
        public Task<GroupDto> CreateAsync(CreateGroupDto dto) => Task.FromResult<GroupDto>(null);
        public Task<GroupDto> UpdateAsync(int id, CreateGroupDto dto) => Task.FromResult<GroupDto>(null);
        public Task<bool> RemoveAsync(int id) => Task.FromResult(false);
        // Members, Votes, Fund methods omitted for brevity
    }
}