using FluentValidation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvCoOwnership.DTOs.AuthDTOs
{
    public class RegisterRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? Phone { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Address { get; set; }
    }

    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("EMAIL_REQUIRED")
                .EmailAddress().WithMessage("INVALID_EMAIL_FORMAT")
                .MaximumLength(255).WithMessage("EMAIL_MAX_255_CHARACTERS");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("PASSWORD_REQUIRED")
                .MinimumLength(8).WithMessage("PASSWORD_MIN_8_CHARACTERS")
                .MaximumLength(100).WithMessage("PASSWORD_MAX_100_CHARACTERS")
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]")
                .WithMessage("PASSWORD_MUST_CONTAIN_UPPERCASE_LOWERCASE_NUMBER_SPECIAL");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("CONFIRM_PASSWORD_REQUIRED")
                .Equal(x => x.Password).WithMessage("CONFIRM_PASSWORD_MUST_MATCH");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("FIRST_NAME_REQUIRED")
                .MaximumLength(50).WithMessage("FIRST_NAME_MAX_50_CHARACTERS")
                .Matches(@"^[a-zA-ZÀ-ỹ\s]+$").WithMessage("FIRST_NAME_ONLY_LETTERS_AND_SPACES");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("LAST_NAME_REQUIRED")
                .MaximumLength(50).WithMessage("LAST_NAME_MAX_50_CHARACTERS")
                .Matches(@"^[a-zA-ZÀ-ỹ\s]+$").WithMessage("LAST_NAME_ONLY_LETTERS_AND_SPACES");

            RuleFor(x => x.Phone)
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("INVALID_PHONE_FORMAT")
                .When(x => !string.IsNullOrEmpty(x.Phone));

            RuleFor(x => x.DateOfBirth)
                .LessThan(DateOnly.FromDateTime(DateTime.Now.AddYears(-16)))
                .WithMessage("MUST_BE_AT_LEAST_16_YEARS_OLD")
                .When(x => x.DateOfBirth.HasValue);

            RuleFor(x => x.Address)
                .MaximumLength(500).WithMessage("ADDRESS_MAX_500_CHARACTERS")
                .When(x => !string.IsNullOrEmpty(x.Address));
        }
    }
}