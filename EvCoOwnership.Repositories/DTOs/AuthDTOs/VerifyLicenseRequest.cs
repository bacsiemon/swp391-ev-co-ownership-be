using FluentValidation;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace EvCoOwnership.Repositories.DTOs.AuthDTOs
{
    /// <summary>
    /// Request DTO for verifying driving license
    /// </summary>
    public class VerifyLicenseRequest
    {
        /// <summary>
        /// Driving license number to verify
        /// </summary>
        public string LicenseNumber { get; set; } = null!;

        /// <summary>
        /// Date when the license was issued
        /// </summary>
        public DateOnly IssueDate { get; set; }

        /// <summary>
        /// Name of the authority that issued the license
        /// </summary>
        public string IssuedBy { get; set; } = null!;

        /// <summary>
        /// License holder's first name
        /// </summary>
        public string FirstName { get; set; } = null!;

        /// <summary>
        /// License holder's last name
        /// </summary>
        public string LastName { get; set; } = null!;

        /// <summary>
        /// License holder's date of birth
        /// </summary>
        public DateOnly DateOfBirth { get; set; }

        /// <summary>
        /// License image file for verification (optional)
        /// </summary>
        public IFormFile? LicenseImage { get; set; }
    }

    /// <summary>
    /// FluentValidation validator for VerifyLicenseRequest
    /// </summary>
    public class VerifyLicenseRequestValidator : AbstractValidator<VerifyLicenseRequest>
    {
        public VerifyLicenseRequestValidator()
        {
            RuleFor(x => x.LicenseNumber)
                .NotEmpty().WithMessage("LICENSE_NUMBER_REQUIRED")
                .Length(6, 15).WithMessage("LICENSE_NUMBER_INVALID_LENGTH")
                .Matches(@"^[A-Z0-9]+$").WithMessage("LICENSE_NUMBER_INVALID_FORMAT");

            RuleFor(x => x.IssueDate)
                .NotEmpty().WithMessage("ISSUE_DATE_REQUIRED")
                .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Now))
                .WithMessage("ISSUE_DATE_CANNOT_BE_FUTURE");

            RuleFor(x => x.IssuedBy)
                .NotEmpty().WithMessage("ISSUED_BY_REQUIRED")
                .MaximumLength(100).WithMessage("ISSUED_BY_MAX_100_CHARACTERS")
                .Matches(@"^[a-zA-ZÀ-ỹ\s,.-]+$").WithMessage("ISSUED_BY_INVALID_FORMAT");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("FIRST_NAME_REQUIRED")
                .MaximumLength(50).WithMessage("FIRST_NAME_MAX_50_CHARACTERS")
                .Matches(@"^[a-zA-ZÀ-ỹ\s]+$").WithMessage("FIRST_NAME_ONLY_LETTERS_AND_SPACES");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("LAST_NAME_REQUIRED")
                .MaximumLength(50).WithMessage("LAST_NAME_MAX_50_CHARACTERS")
                .Matches(@"^[a-zA-ZÀ-ỹ\s]+$").WithMessage("LAST_NAME_ONLY_LETTERS_AND_SPACES");

            RuleFor(x => x.DateOfBirth)
                .NotEmpty().WithMessage("DATE_OF_BIRTH_REQUIRED")
                .LessThan(DateOnly.FromDateTime(DateTime.Now.AddYears(-18)))
                .WithMessage("MUST_BE_AT_LEAST_18_YEARS_OLD")
                .GreaterThan(DateOnly.FromDateTime(DateTime.Now.AddYears(-100)))
                .WithMessage("DATE_OF_BIRTH_TOO_OLD");

            RuleFor(x => x.LicenseImage)
                .Must(BeValidImageFile).WithMessage("INVALID_IMAGE_FILE")
                .Must(BeValidImageSize).WithMessage("IMAGE_SIZE_TOO_LARGE")
                .When(x => x.LicenseImage != null);
        }

        private static bool BeValidImageFile(IFormFile? file)
        {
            if (file == null) return true;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
            var allowedMimeTypes = new[]
            {
                "image/jpeg",
                "image/jpg",
                "image/png",
                "image/gif",
                "image/bmp"
            };

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var mimeType = file.ContentType.ToLowerInvariant();

            return allowedExtensions.Contains(extension) && allowedMimeTypes.Contains(mimeType);
        }

        private static bool BeValidImageSize(IFormFile? file)
        {
            if (file == null) return true;

            // Maximum size: 5MB
            const long maxSizeInBytes = 5 * 1024 * 1024;
            return file.Length <= maxSizeInBytes;
        }
    }
}