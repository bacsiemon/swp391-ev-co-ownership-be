using FluentValidation;

namespace EvCoOwnership.DTOs.TestDTOs
{
    public class TestValidationRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public string? OptionalField { get; set; }
    }

    public class TestValidationRequestValidator : AbstractValidator<TestValidationRequest>
    {
        public TestValidationRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("NAME_REQUIRED")
                .MinimumLength(2).WithMessage("NAME_MIN_2_CHARACTERS")
                .MaximumLength(50).WithMessage("NAME_MAX_50_CHARACTERS");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("EMAIL_REQUIRED")
                .EmailAddress().WithMessage("INVALID_EMAIL_FORMAT");

            RuleFor(x => x.Age)
                .GreaterThan(0).WithMessage("AGE_MUST_BE_POSITIVE")
                .LessThan(150).WithMessage("AGE_MUST_BE_REALISTIC");

            RuleFor(x => x.OptionalField)
                .MaximumLength(100).WithMessage("OPTIONAL_FIELD_MAX_100_CHARACTERS")
                .When(x => !string.IsNullOrEmpty(x.OptionalField));
        }
    }
}