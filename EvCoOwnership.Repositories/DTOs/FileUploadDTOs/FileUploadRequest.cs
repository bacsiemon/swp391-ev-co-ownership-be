using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace EvCoOwnership.Repositories.DTOs.FileUploadDTOs
{
    public class FileUploadRequest
    {
        public IFormFile File { get; set; } = null!;
    }

    public class FileUploadRequestValidator : AbstractValidator<FileUploadRequest>
    {
        public FileUploadRequestValidator()
        {
            RuleFor(x => x.File)
                .NotNull().WithMessage("FILE_REQUIRED")
                .Must(BeValidFileType).WithMessage("INVALID_FILE_TYPE")
                .Must(BeValidFileSize).WithMessage("FILE_SIZE_EXCEEDS_LIMIT");
        }

        private bool BeValidFileType(IFormFile file)
        {
            if (file == null) return false;

            var allowedTypes = new[]
            {
                "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp",
                "application/pdf",
                "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "text/plain"
            };

            return allowedTypes.Contains(file.ContentType.ToLower());
        }

        private bool BeValidFileSize(IFormFile file)
        {
            if (file == null) return false;
            
            // 100MB limit
            const long maxSizeInBytes = 100 * 1024 * 1024;
            return file.Length <= maxSizeInBytes;
        }
    }
}