using FluentValidation;

namespace EvCoOwnership.DTOs.Notifications
{
    /// <summary>
    /// Request DTO for marking a single notification as read
    /// </summary>
    public class MarkNotificationAsReadRequest
    {
        /// <summary>
        /// User notification ID to mark as read
        /// </summary>
        public int UserNotificationId { get; set; }
    }

    /// <summary>
    /// Validator for MarkNotificationAsReadRequest
    /// </summary>
    public class MarkNotificationAsReadRequestValidator : AbstractValidator<MarkNotificationAsReadRequest>
    {
        public MarkNotificationAsReadRequestValidator()
        {
            RuleFor(x => x.UserNotificationId)
                .GreaterThan(0)
                .WithMessage("User notification ID must be greater than 0");
        }
    }
}