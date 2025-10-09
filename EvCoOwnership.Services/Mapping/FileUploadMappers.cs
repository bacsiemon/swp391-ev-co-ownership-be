using EvCoOwnership.DTOs.FileUploadDTOs;
using EvCoOwnership.Repositories.Models;

namespace EvCoOwnership.Services.Mapping
{
    public static class FileUploadMappers
    {
        #region DTO to Entity

        public static async Task<FileUpload> ToEntityAsync(this FileUploadRequest request)
        {
            if (request?.File == null)
                throw new ArgumentNullException(nameof(request));

            byte[] fileData;
            using (var memoryStream = new MemoryStream())
            {
                await request.File.CopyToAsync(memoryStream);
                fileData = memoryStream.ToArray();
            }

            return new FileUpload
            {
                Data = fileData,
                FileName = request.File.FileName,
                MimeType = request.File.ContentType,
                UploadedAt = DateTime.UtcNow
            };
        }

        #endregion

        #region Entity to DTO

        public static FileUploadResponse ToResponse(this FileUpload entity, string baseUrl)
        {
            return new FileUploadResponse
            {
                Id = entity.Id,
                FileName = entity.FileName,
                MimeType = entity.MimeType,
                FileSize = entity.Data?.Length ?? 0,
                UploadedAt = entity.UploadedAt,
                DownloadUrl = $"{baseUrl}/api/fileupload/{entity.Id}"
            };
        }

        public static FileDownloadResponse ToDownloadResponse(this FileUpload entity)
        {
            return new FileDownloadResponse
            {
                Data = entity.Data,
                FileName = entity.FileName,
                MimeType = entity.MimeType
            };
        }

        #endregion
    }
}