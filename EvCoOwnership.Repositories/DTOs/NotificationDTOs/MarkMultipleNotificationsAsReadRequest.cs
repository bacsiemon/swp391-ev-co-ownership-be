using FluentValidation;

namespace EvCoOwnership.Repositories.DTOs.NotificationDTOs
{
    /// <summary>
    /// Request DTO for marking multiple notifications as read
    /// </summary>
    public class MarkMultipleNotificationsAsReadRequest
    {
        /// <summary>
        /// List of user notification IDs to mark as read
        /// </summary>
        public List<int> UserNotificationIds { get; set; } = new();
    }

    /// <summary>
    /// Validator for MarkMultipleNotificationsAsReadRequest
    /// </summary>
    public class MarkMultipleNotificationsAsReadRequestValidator : AbstractValidator<MarkMultipleNotificationsAsReadRequest>
    {
        public MarkMultipleNotificationsAsReadRequestValidator()
        {
            RuleFor(x => x.UserNotificationIds)
                .NotEmpty()
                .WithMessage("At least one user notification ID is required")
                .Must(ids => ids.Count <= 100)
                .WithMessage("Cannot mark more than 100 notifications at once");

            RuleForEach(x => x.UserNotificationIds)
                .GreaterThan(0)
                .WithMessage("User notification ID must be greater than 0");

            RuleFor(x => x.UserNotificationIds)
                .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("User notification IDs must be unique");
        }
    }
}