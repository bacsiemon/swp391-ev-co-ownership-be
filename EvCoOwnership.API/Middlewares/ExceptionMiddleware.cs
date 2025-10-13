using EvCoOwnership.Helpers.BaseClasses;
using System.Text.Json;

namespace EvCoOwnership.API.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception: {ex}");
                httpContext.Response.StatusCode = 500;
                httpContext.Response.ContentType = "application/json";
                await httpContext.Response.WriteAsJsonAsync(new BaseResponse
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.ToString()
                });
            }
        }
    }
}
