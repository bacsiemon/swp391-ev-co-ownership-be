using EvCoOwnership.Repositories.Enums;
using FluentValidation;

namespace EvCoOwnership.DTOs.Notifications
{
    /// <summary>
    /// Request DTO for creating a new notification
    /// </summary>
    public class CreateNotificationRequest
    {
        public string NotificationType { get; set; } = string.Empty;
        public ESeverityType Priority { get; set; } = ESeverityType.Low;
        public string? AdditionalData { get; set; }
        public List<int> UserIds { get; set; } = new();
    }

    /// <summary>
    /// Validator for CreateNotificationRequest
    /// </summary>
    public class CreateNotificationRequestDtoValidator : AbstractValidator<CreateNotificationRequest>
    {
        public CreateNotificationRequestDtoValidator()
        {
            RuleFor(x => x.NotificationType)
                .NotEmpty()
                .WithMessage("Notification type is required")
                .MaximumLength(500)
                .WithMessage("Notification type must not exceed 500 characters");

            RuleFor(x => x.Priority)
                .IsInEnum()
                .WithMessage("Priority must be a valid severity type");

            RuleFor(x => x.UserIds)
                .NotEmpty()
                .WithMessage("At least one user ID is required");

            RuleForEach(x => x.UserIds)
                .GreaterThan(0)
                .WithMessage("User ID must be greater than 0");

            RuleFor(x => x.AdditionalData)
                .MaximumLength(2000)
                .WithMessage("Additional data must not exceed 2000 characters");
        }
    }
}