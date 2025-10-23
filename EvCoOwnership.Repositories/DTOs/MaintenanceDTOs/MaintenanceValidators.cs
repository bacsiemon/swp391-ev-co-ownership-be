using FluentValidation;

namespace EvCoOwnership.Repositories.DTOs.MaintenanceDTOs
{
    public class CreateMaintenanceValidator : AbstractValidator<CreateMaintenanceRequest>
    {
        public CreateMaintenanceValidator()
        {
            RuleFor(x => x.VehicleId)
                .GreaterThan(0).WithMessage("VEHICLE_ID_REQUIRED");

            RuleFor(x => x.MaintenanceType)
                .IsInEnum().WithMessage("INVALID_MAINTENANCE_TYPE");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("DESCRIPTION_REQUIRED")
                .Length(5, 1000).WithMessage("DESCRIPTION_MUST_BE_BETWEEN_5_AND_1000_CHARACTERS");

            RuleFor(x => x.Cost)
                .GreaterThan(0).WithMessage("COST_MUST_BE_GREATER_THAN_ZERO")
                .LessThanOrEqualTo(1000000000).WithMessage("COST_EXCEEDS_MAXIMUM_LIMIT");

            RuleFor(x => x.ServiceProvider)
                .NotEmpty().WithMessage("SERVICE_PROVIDER_REQUIRED")
                .Length(2, 200).WithMessage("SERVICE_PROVIDER_MUST_BE_BETWEEN_2_AND_200_CHARACTERS");

            RuleFor(x => x.ServiceDate)
                .NotEmpty().WithMessage("SERVICE_DATE_REQUIRED")
                .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Now))
                .WithMessage("SERVICE_DATE_CANNOT_BE_IN_FUTURE");

            When(x => x.OdometerReading.HasValue, () =>
            {
                RuleFor(x => x.OdometerReading!.Value)
                    .GreaterThan(0).WithMessage("ODOMETER_READING_MUST_BE_GREATER_THAN_ZERO");
            });
        }
    }

    public class UpdateMaintenanceValidator : AbstractValidator<UpdateMaintenanceRequest>
    {
        public UpdateMaintenanceValidator()
        {
            When(x => x.MaintenanceType.HasValue, () =>
            {
                RuleFor(x => x.MaintenanceType!.Value)
                    .IsInEnum().WithMessage("INVALID_MAINTENANCE_TYPE");
            });

            When(x => !string.IsNullOrWhiteSpace(x.Description), () =>
            {
                RuleFor(x => x.Description)
                    .Length(5, 1000).WithMessage("DESCRIPTION_MUST_BE_BETWEEN_5_AND_1000_CHARACTERS");
            });

            When(x => x.Cost.HasValue, () =>
            {
                RuleFor(x => x.Cost!.Value)
                    .GreaterThan(0).WithMessage("COST_MUST_BE_GREATER_THAN_ZERO");
            });

            When(x => !string.IsNullOrWhiteSpace(x.ServiceProvider), () =>
            {
                RuleFor(x => x.ServiceProvider)
                    .Length(2, 200).WithMessage("SERVICE_PROVIDER_MUST_BE_BETWEEN_2_AND_200_CHARACTERS");
            });

            When(x => x.ServiceDate.HasValue, () =>
            {
                RuleFor(x => x.ServiceDate!.Value)
                    .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Now))
                    .WithMessage("SERVICE_DATE_CANNOT_BE_IN_FUTURE");
            });

            When(x => x.OdometerReading.HasValue, () =>
            {
                RuleFor(x => x.OdometerReading!.Value)
                    .GreaterThan(0).WithMessage("ODOMETER_READING_MUST_BE_GREATER_THAN_ZERO");
            });
        }
    }
}
