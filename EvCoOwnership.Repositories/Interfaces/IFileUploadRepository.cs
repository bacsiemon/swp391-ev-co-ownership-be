using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;

namespace EvCoOwnership.Repositories.Interfaces
{
    public interface IFileUploadRepository : IGenericRepository<FileUpload>
    {
        Task<FileUpload?> GetByIdAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}