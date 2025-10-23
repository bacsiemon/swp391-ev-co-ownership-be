using EvCoOwnership.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EvCoOwnership.Services.BackgroundServices
{
    /// <summary>
    /// Background service that periodically checks and sends booking reminders
    /// </summary>
    public class BookingReminderBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BookingReminderBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15); // Check every 15 minutes

        public BookingReminderBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<BookingReminderBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Booking Reminder Background Service started");

            // Wait 1 minute before starting to allow application to fully initialize
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessRemindersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing booking reminders");
                }

                // Wait for next check interval
                try
                {
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Service is stopping
                    break;
                }
            }

            _logger.LogInformation("Booking Reminder Background Service stopped");
        }

        private async Task ProcessRemindersAsync()
        {
            _logger.LogDebug("Starting booking reminder check");

            try
            {
                // Create a new scope to get scoped services
                using var scope = _serviceProvider.CreateScope();
                var reminderService = scope.ServiceProvider.GetRequiredService<IBookingReminderService>();

                var remindersSent = await reminderService.ProcessPendingRemindersAsync();

                if (remindersSent > 0)
                {
                    _logger.LogInformation("Booking reminder check completed: {Count} reminders sent", remindersSent);
                }
                else
                {
                    _logger.LogDebug("Booking reminder check completed: No reminders to send");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessRemindersAsync");
            }
        }
    }
}
