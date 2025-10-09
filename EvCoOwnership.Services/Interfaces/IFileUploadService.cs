using EvCoOwnership.DTOs.FileUploadDTOs;
using EvCoOwnership.Helpers.BaseClasses;
using Microsoft.AspNetCore.Http;

namespace EvCoOwnership.Services.Interfaces
{
    public interface IFileUploadService
    {
        Task<BaseResponse> UploadFileAsync(FileUploadRequest request);
        Task<BaseResponse> GetFileAsync(int id);
        Task<BaseResponse> DeleteFileAsync(int id);
        Task<BaseResponse> GetFileInfoAsync(int id);


        Task<FileUploadResponse> UploadFileAsync(IFormFile file);
    }
}