using FluentValidation;

namespace EvCoOwnership.Repositories.DTOs.NotificationDTOs
{
    /// <summary>
    /// Request DTO for creating a new notification
    /// </summary>
    public class CreateNotificationRequest
    {
        public string NotificationType { get; set; }
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
                .MaximumLength(50)
                .WithMessage("Notification type must not exceed 50 characters");

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