using FluentValidation;

namespace EvCoOwnership.Repositories.DTOs.NotificationDTOs
{
    /// <summary>
    /// Request DTO for sending a notification to a specific user
    /// </summary>
    public class SendNotificationRequestDto
    {
        public int UserId { get; set; }
        public string NotificationType { get; set; }
        public string? AdditionalData { get; set; }
    }

    /// <summary>
    /// Validator for SendNotificationRequestDto
    /// </summary>
    public class SendNotificationRequestDtoValidator : AbstractValidator<SendNotificationRequestDto>
    {
        public SendNotificationRequestDtoValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0)
                .WithMessage("User ID must be greater than 0");

            RuleFor(x => x.NotificationType)
                .NotEmpty()
                .WithMessage("Notification type is required")
                .MaximumLength(50)
                .WithMessage("Notification type must not exceed 50 characters");

            RuleFor(x => x.AdditionalData)
                .MaximumLength(2000)
                .WithMessage("Additional data must not exceed 2000 characters");
        }
    }
}