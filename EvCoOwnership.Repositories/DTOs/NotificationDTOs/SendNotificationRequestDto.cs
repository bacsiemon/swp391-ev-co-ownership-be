using FluentValidation;

namespace EvCoOwnership.DTOs.Notifications
{
    /// <summary>
    /// Request DTO for sending a notification to a specific user
    /// </summary>
    public class SendNotificationRequestDto
    {
        public int UserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string NotificationType { get; set; } = string.Empty;
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

            RuleFor(x => x.Message)
                .NotEmpty()
                .WithMessage("Message is required")
                .MaximumLength(1000)
                .WithMessage("Message must not exceed 1000 characters");

            RuleFor(x => x.NotificationType)
                .NotEmpty()
                .WithMessage("Notification type is required")
                .MaximumLength(500)
                .WithMessage("Notification type must not exceed 500 characters");

            RuleFor(x => x.AdditionalData)
                .MaximumLength(2000)
                .WithMessage("Additional data must not exceed 2000 characters");
        }
    }
}