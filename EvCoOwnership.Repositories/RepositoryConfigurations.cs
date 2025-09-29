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
            services.AddDbContext(configuration);
            return services;
        }

        public static void AddDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            string connectionString = configuration.GetConnectionString("AzureDBConnection");
#if DEBUG
            connectionString = configuration.GetConnectionString("LocalConnection");
#endif
            services.AddDbContext<EvCoOwnershipDbContext>(options =>
            options.UseNpgsql(connectionString));
        }
    }
}
