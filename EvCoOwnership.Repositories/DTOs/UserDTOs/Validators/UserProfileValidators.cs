using FluentValidation;
using EvCoOwnership.Repositories.DTOs.UserDTOs;

namespace EvCoOwnership.Repositories.DTOs.UserDTOs.Validators
{
    /// <summary>
    /// Validator for UpdateUserProfileRequest
    /// </summary>
    public class UpdateUserProfileRequestValidator : AbstractValidator<UpdateUserProfileRequest>
    {
        public UpdateUserProfileRequestValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("FIRST_NAME_REQUIRED")
                .MaximumLength(50).WithMessage("FIRST_NAME_MAX_50_CHARACTERS")
                .Matches(@"^[a-zA-ZÀ-ỹ\s]+$").WithMessage("FIRST_NAME_ONLY_LETTERS_AND_SPACES");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("LAST_NAME_REQUIRED")
                .MaximumLength(50).WithMessage("LAST_NAME_MAX_50_CHARACTERS")
                .Matches(@"^[a-zA-ZÀ-ỹ\s]+$").WithMessage("LAST_NAME_ONLY_LETTERS_AND_SPACES");

            RuleFor(x => x.Phone)
                .Matches(@"^(\+84|0)[3-9]\d{8}$").WithMessage("INVALID_VIETNAM_PHONE_FORMAT")
                .When(x => !string.IsNullOrEmpty(x.Phone));

            RuleFor(x => x.DateOfBirth)
                .Must(BeValidAge).WithMessage("MUST_BE_AT_LEAST_18_YEARS_OLD")
                .When(x => x.DateOfBirth.HasValue);

            RuleFor(x => x.Address)
                .MaximumLength(200).WithMessage("ADDRESS_MAX_200_CHARACTERS")
                .When(x => !string.IsNullOrEmpty(x.Address));
        }

        private bool BeValidAge(DateOnly? dateOfBirth)
        {
            if (!dateOfBirth.HasValue) return true;
            
            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = today.Year - dateOfBirth.Value.Year;
            
            if (dateOfBirth.Value.AddYears(age) > today)
                age--;
                
            return age >= 18;
        }
    }

    /// <summary>
    /// Validator for ChangePasswordRequest
    /// </summary>
    public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
    {
        public ChangePasswordRequestValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("CURRENT_PASSWORD_REQUIRED");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("NEW_PASSWORD_REQUIRED")
                .MinimumLength(8).WithMessage("NEW_PASSWORD_MIN_8_CHARACTERS")
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]")
                .WithMessage("NEW_PASSWORD_MUST_CONTAIN_UPPERCASE_LOWERCASE_NUMBER_SPECIAL");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("CONFIRM_PASSWORD_REQUIRED")
                .Equal(x => x.NewPassword).WithMessage("CONFIRM_PASSWORD_MUST_MATCH");
        }
    }
}