# Serilog Configuration Guide

This project has been configured with Serilog for comprehensive logging. Here's what has been set up:

## Installed Packages

- `Serilog.AspNetCore` - Core Serilog integration for ASP.NET Core
- `Serilog.Sinks.Console` - Console output sink
- `Serilog.Sinks.File` - File output sink with rolling functionality
- `Serilog.Sinks.Seq` - Structured logging to Seq server (optional)

## Configuration Files

### appsettings.json (Production/Default)
- **Log Level**: Information and above
- **Sinks**: Console, File, Seq
- **File Location**: `logs/log-.txt` (daily rolling)
- **File Size Limit**: 10MB with 30-day retention

### appsettings.Development.json
- **Log Level**: Debug and above for more detailed logging
- **Additional Sink**: Debug output for Visual Studio
- **File Location**: `logs/development-log-.txt` (7-day retention)

### appsettings.Production.json
- **Log Level**: Warning and above for performance
- **Reduced logging**: Only errors and warnings from framework code
- **File Size Limit**: 50MB with 90-day retention

## Features Configured

1. **Structured Logging**: All logs are structured with proper context
2. **Request Logging**: HTTP requests are automatically logged with timing
3. **Error Handling**: Proper exception logging in controllers
4. **Enrichment**: Logs include machine name, thread ID, and request context
5. **Performance**: Different log levels for different environments

## Usage Examples

### In Controllers
```csharp
private readonly ILogger _logger;

public MyController(ILogger logger)
{
    _logger = logger;
}

public async Task<IActionResult> MyAction(int id)
{
    _logger.Information("Processing request for ID: {Id}", id);
    
    try
    {
        // Your logic here
        _logger.Information("Successfully processed ID: {Id}", id);
    }
    catch (Exception ex)
    {
        _logger.Error(ex, "Error processing ID: {Id}", id);
    }
}
```

### Log Levels
- **Debug**: Detailed information for debugging
- **Information**: General application flow
- **Warning**: Something unexpected but not critical
- **Error**: Error occurred but application continues
- **Fatal**: Critical errors that may cause application to terminate

## Optional: Seq Server Setup

To use the Seq sink for advanced log analysis:

1. Install Seq server: https://datalust.co/seq
2. Update the Seq server URL in appsettings.json if needed
3. Seq provides a web UI for searching and analyzing structured logs

## Log File Locations

- Development: `logs/development-log-{date}.txt`
- Production: `logs/production-log-{date}.txt`
- Default: `logs/log-{date}.txt`

## Best Practices

1. Use structured logging with properties: `_logger.Information("User {UserId} performed action {Action}", userId, action)`
2. Don't log sensitive information (passwords, tokens, etc.)
3. Use appropriate log levels
4. Log method entry/exit for critical operations
5. Always log exceptions with context