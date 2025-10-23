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
                .NotEmpty().WithMessage("PAYMENT_GATEWAY_REQUIRED")
                .Must(x => new[] { "VNPay", "MoMo", "ZaloPay", "Banking" }.Contains(x))
                .WithMessage("INVALID_PAYMENT_GATEWAY");
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
