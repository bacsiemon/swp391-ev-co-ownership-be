using EvCoOwnership.API;
using EvCoOwnership.Helpers;
using EvCoOwnership.API.Middlewares;
using EvCoOwnership.API.Extensions;
using EvCoOwnership.API.Hubs;
using EvCoOwnership.Repositories;
using EvCoOwnership.Repositories.Context;
using EvCoOwnership.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using System.Reflection;

// Create initial bootstrap logger
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting EvCoOwnership API application");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    // Add services to the container.
    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();

    // Add FluentValidation services
    builder.Services.AddFluentValidationServices();

    builder.Services.AddApiConfigurations(builder.Configuration);
    builder.Services.AddServiceConfigurations(builder.Configuration);
    builder.Services.AddRepositoryConfigurations(builder.Configuration);
    builder.Services.AddHelperConfigurations(builder.Configuration);
    builder.Services.AddDbContext<EvCoOwnershipDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


    var app = builder.Build();

    // Configure Serilog request logging
    app.UseSerilogRequestLogging(options =>
    {
        // Customize the message template
        options.MessageTemplate = "Handled {RequestPath} in {Elapsed:0.0000} ms";

        // Emit debug-level events instead of the defaults
        options.GetLevel = (httpContext, elapsed, ex) => LogEventLevel.Debug;

        // Attach additional properties to the request completion event
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        };
    });

    app.UseMiddleware<ExceptionMiddleware>();

    // Configure the HTTP request pipeline.
    if (true)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {

        });
    }

    app.UseCors(options =>
    {
        options.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });

    app.UseHttpsRedirection();

    // Custom authorization middleware (must be before UseAuthentication and UseAuthorization)
    app.UseMiddleware<AuthorizationMiddleware>();
    app.UseMiddleware<ExceptionMiddleware>();

    app.UseAuthentication();
    app.UseAuthorization();

    // Add notification middleware to listen for events
    app.UseNotificationMiddleware();

    app.MapControllers();
    
    // Map SignalR hub
    app.MapHub<NotificationHub>("/notificationHub");

    app.UseCors(options => options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

    Log.Information("EvCoOwnership API application started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "EvCoOwnership API application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
