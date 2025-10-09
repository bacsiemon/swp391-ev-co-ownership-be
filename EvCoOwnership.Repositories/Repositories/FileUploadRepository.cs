using EvCoOwnership.Repositories.Context;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace EvCoOwnership.Repositories.Repositories
{
    public class FileUploadRepository : GenericRepository<FileUpload>, IFileUploadRepository
    {
        public FileUploadRepository(EvCoOwnershipDbContext context) : base(context)
        {
        }

        public async Task<FileUpload?> GetByIdAsync(int id)
        {
            return await _context.FileUploads
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.FileUploads
                .AnyAsync(f => f.Id == id);
        }
    }
}