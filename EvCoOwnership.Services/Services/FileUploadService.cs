using EvCoOwnership.Repositories.DTOs.FileUploadDTOs;
using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Services.Interfaces;
using EvCoOwnership.Services.Mapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EvCoOwnership.Services.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<FileUploadService> _logger;
        private readonly IConfiguration _configuration;

        public FileUploadService(IUnitOfWork unitOfWork, ILogger<FileUploadService> logger, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<BaseResponse> UploadFileAsync(FileUploadRequest request)
        {
            return new BaseResponse() 
            { 
                StatusCode = 200, 
                Message = "SUCCESS",
                Data = await UploadFileAsync(request.File)
            };
        }

        public async Task<FileUploadResponse> UploadFileAsync(IFormFile file)
        {
            // Get baseUrl from configuration
            var configuredBaseUrl = _configuration["AppSettings:BaseUrl"];
            var fileUpload = await new FileUploadRequest
            {
                File = file
            }.ToEntityAsync();

            // Save to database
            _unitOfWork.FileUploadRepository.Create(fileUpload);
            await _unitOfWork.SaveChangesAsync();

            return fileUpload.ToResponse(configuredBaseUrl);
        }

        public async Task<BaseResponse> GetFileAsync(int id)
        {
            var fileUpload = await _unitOfWork.FileUploadRepository.GetByIdAsync(id);
            if (fileUpload == null)
            {
                return new BaseResponse
                {
                    StatusCode = 404,
                    Message = "FILE_NOT_FOUND"
                };
            }

            var response = fileUpload.ToDownloadResponse();

            return new BaseResponse
            {
                StatusCode = 200,
                Message = "SUCCESS",
                Data = response
            };
        }

        public async Task<BaseResponse> DeleteFileAsync(int id)
        {
            var fileUpload = await _unitOfWork.FileUploadRepository.GetByIdAsync(id);
            if (fileUpload == null)
            {
                return new BaseResponse
                {
                    StatusCode = 404,
                    Message = "FILE_NOT_FOUND"
                };
            }

            _unitOfWork.FileUploadRepository.Remove(fileUpload);
            await _unitOfWork.SaveChangesAsync();

            return new BaseResponse
            {
                StatusCode = 200,
                Message = "SUCCESS"
            };  
        }

        public async Task<BaseResponse> GetFileInfoAsync(int id)
        {
            // Get baseUrl from configuration 
            var configuredBaseUrl = _configuration["AppSettings:BaseUrl"];

            var fileUpload = await _unitOfWork.FileUploadRepository.GetByIdAsync(id);
            if (fileUpload == null)
            {
                return new BaseResponse
                {
                    StatusCode = 404,
                    Message = "FILE_NOT_FOUND"
                };
            }

            var response = fileUpload.ToResponse(configuredBaseUrl);

            return new BaseResponse
            {
                StatusCode = 200,
                Message = "SUCCESS",
                Data = response
            };
        }
    }
}