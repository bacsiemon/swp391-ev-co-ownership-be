using FluentValidation;

namespace EvCoOwnership.Repositories.DTOs.BookingDTOs
{
    public class CreateBookingValidator : AbstractValidator<CreateBookingRequest>
    {
        public CreateBookingValidator()
        {
            RuleFor(x => x.VehicleId)
                .GreaterThan(0).WithMessage("VEHICLE_ID_REQUIRED");

            RuleFor(x => x.StartTime)
                .NotEmpty().WithMessage("START_TIME_REQUIRED")
                .GreaterThanOrEqualTo(DateTime.Now).WithMessage("START_TIME_MUST_BE_IN_FUTURE");

            RuleFor(x => x.EndTime)
                .NotEmpty().WithMessage("END_TIME_REQUIRED")
                .GreaterThan(x => x.StartTime).WithMessage("END_TIME_MUST_BE_AFTER_START_TIME");

            RuleFor(x => x.Purpose)
                .NotEmpty().WithMessage("PURPOSE_REQUIRED")
                .Length(5, 500).WithMessage("PURPOSE_MUST_BE_BETWEEN_5_AND_500_CHARACTERS");

            // Booking duration should be reasonable (minimum 1 hour, maximum 30 days)
            RuleFor(x => x)
                .Must(x => (x.EndTime - x.StartTime).TotalHours >= 1)
                .WithMessage("BOOKING_DURATION_MINIMUM_1_HOUR")
                .Must(x => (x.EndTime - x.StartTime).TotalDays <= 30)
                .WithMessage("BOOKING_DURATION_MAXIMUM_30_DAYS");
        }
    }

    public class UpdateBookingValidator : AbstractValidator<UpdateBookingRequest>
    {
        public UpdateBookingValidator()
        {
            When(x => x.StartTime.HasValue, () =>
            {
                RuleFor(x => x.StartTime!.Value)
                    .GreaterThanOrEqualTo(DateTime.Now).WithMessage("START_TIME_MUST_BE_IN_FUTURE");
            });

            When(x => x.EndTime.HasValue && x.StartTime.HasValue, () =>
            {
                RuleFor(x => x.EndTime!.Value)
                    .GreaterThan(x => x.StartTime!.Value).WithMessage("END_TIME_MUST_BE_AFTER_START_TIME");
            });

            When(x => !string.IsNullOrWhiteSpace(x.Purpose), () =>
            {
                RuleFor(x => x.Purpose)
                    .Length(5, 500).WithMessage("PURPOSE_MUST_BE_BETWEEN_5_AND_500_CHARACTERS");
            });
        }
    }

    public class ApproveBookingValidator : AbstractValidator<ApproveBookingRequest>
    {
        public ApproveBookingValidator()
        {
            When(x => !x.IsApproved, () =>
            {
                RuleFor(x => x.RejectionReason)
                    .NotEmpty().WithMessage("REJECTION_REASON_REQUIRED")
                    .Length(5, 500).WithMessage("REJECTION_REASON_MUST_BE_BETWEEN_5_AND_500_CHARACTERS");
            });
        }
    }
}
