using FluentValidation;

namespace EvCoOwnership.DTOs.Notifications
{
    /// <summary>
    /// Request DTO for marking notifications as read
    /// </summary>
    public class MarkNotificationReadRequest
    {
        public List<int> NotificationIds { get; set; } = new();
    }

    /// <summary>
    /// Validator for MarkNotificationReadRequest
    /// </summary>
    public class MarkNotificationReadRequestDtoValidator : AbstractValidator<MarkNotificationReadRequest>
    {
        public MarkNotificationReadRequestDtoValidator()
        {
            RuleFor(x => x.NotificationIds)
                .NotEmpty()
                .WithMessage("At least one notification ID is required");

            RuleForEach(x => x.NotificationIds)
                .GreaterThan(0)
                .WithMessage("Notification ID must be greater than 0");
        }
    }
}