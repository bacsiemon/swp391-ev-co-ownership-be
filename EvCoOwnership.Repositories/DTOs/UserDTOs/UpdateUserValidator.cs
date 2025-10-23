using FluentValidation;
using System.Text.RegularExpressions;

namespace EvCoOwnership.Repositories.DTOs.UserDTOs
{
    public class UpdateUserValidator : AbstractValidator<UpdateUserRequest>
    {
        public UpdateUserValidator()
        {
            // Full Name validation - optional but if provided must be 2-100 characters
            When(x => !string.IsNullOrWhiteSpace(x.FullName), () =>
            {
                RuleFor(x => x.FullName)
                    .Length(2, 100).WithMessage("FULLNAME_MUST_BE_BETWEEN_2_AND_100_CHARACTERS")
                    .Matches(@"^[\p{L}\s]+$").WithMessage("FULLNAME_CONTAINS_INVALID_CHARACTERS");
            });

            // Phone Number validation - Vietnamese format
            When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber), () =>
            {
                RuleFor(x => x.PhoneNumber)
                    .Matches(@"^(0[3|5|7|8|9])+([0-9]{8})$").WithMessage("INVALID_VIETNAMESE_PHONE_NUMBER_FORMAT");
            });

            // Address validation - optional but if provided must be valid
            When(x => !string.IsNullOrWhiteSpace(x.Address), () =>
            {
                RuleFor(x => x.Address)
                    .Length(5, 200).WithMessage("ADDRESS_MUST_BE_BETWEEN_5_AND_200_CHARACTERS");
            });

            // Date of Birth validation - must be at least 18 years old
            When(x => x.DateOfBirth.HasValue, () =>
            {
                RuleFor(x => x.DateOfBirth!.Value)
                    .LessThan(DateOnly.FromDateTime(DateTime.Now.AddYears(-18)))
                    .WithMessage("USER_MUST_BE_AT_LEAST_18_YEARS_OLD");
            });

            // Citizen ID validation - Vietnamese CCCD/CMND format
            When(x => !string.IsNullOrWhiteSpace(x.CitizenId), () =>
            {
                RuleFor(x => x.CitizenId)
                    .Matches(@"^(\d{9}|\d{12})$").WithMessage("INVALID_CITIZEN_ID_FORMAT");
            });
        }
    }
}
