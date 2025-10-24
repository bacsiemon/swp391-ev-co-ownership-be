using EvCoOwnership.Repositories.DTOs.BookingDTOs;
using FluentValidation;

namespace EvCoOwnership.Repositories.DTOs.BookingDTOs
{
    /// <summary>
    /// Validator for ConfigureReminderRequest
    /// </summary>
    public class ConfigureReminderRequestValidator : AbstractValidator<ConfigureReminderRequest>
    {
        public ConfigureReminderRequestValidator()
        {
            RuleFor(x => x.HoursBeforeBooking)
                .GreaterThan(0)
                .WithMessage("Hours before booking must be greater than 0")
                .LessThanOrEqualTo(168)
                .WithMessage("Hours before booking must not exceed 168 hours (7 days)");
        }
    }
}
