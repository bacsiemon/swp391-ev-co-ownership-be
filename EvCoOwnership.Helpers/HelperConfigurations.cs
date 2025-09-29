using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvCoOwnership.Helpers
{
    public static class HelperConfigurations
    {
        public static IServiceCollection AddHelperConfigurations(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }
    }
}
