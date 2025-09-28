using EvCoOwnership.Repositories.Context;
using EvCoOwnership.Repositories.UoW;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvCoOwnership.Repositories
{
    public static class RepositoryConfigurations
    {
        public static IServiceCollection AddRepositoryConfigurations(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDependencies(configuration);
            return services;
        }

        public static void AddDependencies(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            var connectionString = configuration.GetConnectionString("LocalConnection");
            services.AddDbContext<EvCoOwnershipDbContext>(options =>
                options.UseNpgsql(connectionString));
        }
    }
}
