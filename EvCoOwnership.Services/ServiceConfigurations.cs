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
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ILicenseVerificationService, LicenseVerificationService>();
            services.AddScoped<ICoOwnerEligibilityService, CoOwnerEligibilityService>();
            services.AddScoped<IFileUploadService, FileUploadService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IVehicleService, VehicleService>();
            services.AddScoped<IUserProfileService, UserProfileService>();
        }
    }
}
