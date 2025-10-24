using EvCoOwnership.Repositories.Enums;
using FluentValidation;

namespace EvCoOwnership.Repositories.DTOs.PaymentDTOs
{
    public class CreatePaymentValidator : AbstractValidator<CreatePaymentRequest>
    {
        public CreatePaymentValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("AMOUNT_MUST_BE_GREATER_THAN_ZERO")
                .LessThanOrEqualTo(1000000000).WithMessage("AMOUNT_EXCEEDS_MAXIMUM_LIMIT");

            RuleFor(x => x.PaymentGateway)
                .IsInEnum().WithMessage("INVALID_PAYMENT_GATEWAY");

            RuleFor(x => x.PaymentType)
                .IsInEnum().WithMessage("INVALID_PAYMENT_TYPE");

            RuleFor(x => x.PaymentMethod)
                .IsInEnum().WithMessage("INVALID_PAYMENT_METHOD")
                .When(x => x.PaymentMethod.HasValue);

            RuleFor(x => x.BankCode)
                .MaximumLength(50).WithMessage("BANK_CODE_TOO_LONG")
                .When(x => !string.IsNullOrEmpty(x.BankCode));

            RuleFor(x => x.EWalletProvider)
                .MaximumLength(50).WithMessage("EWALLET_PROVIDER_TOO_LONG")
                .When(x => !string.IsNullOrEmpty(x.EWalletProvider));

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("DESCRIPTION_TOO_LONG")
                .When(x => !string.IsNullOrEmpty(x.Description));
        }
    }

    public class ProcessPaymentValidator : AbstractValidator<ProcessPaymentRequest>
    {
        public ProcessPaymentValidator()
        {
            RuleFor(x => x.PaymentId)
                .GreaterThan(0).WithMessage("PAYMENT_ID_REQUIRED");

            RuleFor(x => x.TransactionId)
                .NotEmpty().WithMessage("TRANSACTION_ID_REQUIRED")
                .Length(1, 100).WithMessage("TRANSACTION_ID_MUST_BE_BETWEEN_1_AND_100_CHARACTERS");
        }
    }
}
