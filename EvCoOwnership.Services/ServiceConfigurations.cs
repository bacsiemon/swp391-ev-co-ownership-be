using EvCoOwnership.Helpers.Configuration;
using EvCoOwnership.Services.Interfaces;
using EvCoOwnership.Services.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvCoOwnership.Services
{
    public static class ServiceConfigurations
    {
        public static IServiceCollection AddServiceConfigurations(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDependencies(configuration);
            return services;
        }

        public static void AddDependencies(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure VNPay settings
            services.Configure<VnPayConfig>(configuration.GetSection("VnPayConfig"));

            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ILicenseVerificationService, LicenseVerificationService>();
            services.AddScoped<ICoOwnerEligibilityService, CoOwnerEligibilityService>();
            services.AddScoped<IFileUploadService, FileUploadService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IVehicleService, VehicleService>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IMaintenanceService, MaintenanceService>();
            services.AddScoped<IUserProfileService, UserProfileService>();
            services.AddScoped<IVnPayService, VnPayService>();
            // services.AddScoped<IOwnershipChangeService, OwnershipChangeService>();
            services.AddScoped<IUsageAnalyticsService, UsageAnalyticsService>();
            services.AddScoped<ICheckInCheckOutService, CheckInCheckOutService>();
            services.AddScoped<IFundService, FundService>();
            services.AddScoped<IMaintenanceVoteService, MaintenanceVoteService>();
            services.AddScoped<IDisputeService, DisputeService>();
            services.AddScoped<IContractService, ContractService>();
            services.AddScoped<IGroupService, GroupService>();
            services.AddScoped<IScheduleService, ScheduleService>();
            services.AddScoped<IProfileService, ProfileService>();
            services.AddScoped<IGroupManagementService, GroupManagementService>();
        }
    }
}
