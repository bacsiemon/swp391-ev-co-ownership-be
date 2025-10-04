using FluentValidation;
using EvCoOwnership.API.Filters;
using System.Reflection;

namespace EvCoOwnership.API.Extensions
{
    /// <summary>
    /// Extension methods for configuring FluentValidation services
    /// </summary>
    public static class FluentValidationExtensions
    {
        /// <summary>
        /// Adds FluentValidation services and automatic validation filter
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddFluentValidationServices(this IServiceCollection services)
        {
            // Register all validators from Repositories assembly
            var repositoriesAssembly = Assembly.Load("EvCoOwnership.Repositories");
            services.AddValidatorsFromAssembly(repositoriesAssembly);

            // Register the custom validation action filter
            services.AddScoped<ValidationActionFilter>();

            // Configure controllers to use the validation filter
            services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
            {
                options.Filters.AddService<ValidationActionFilter>();
            });

            return services;
        }
    }
}