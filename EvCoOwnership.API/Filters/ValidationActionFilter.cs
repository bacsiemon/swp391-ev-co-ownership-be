using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.API.Attributes;

namespace EvCoOwnership.API.Filters
{
    /// <summary>
    /// Action filter that automatically validates request DTOs using FluentValidation
    /// </summary>
    public class ValidationActionFilter : IActionFilter
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ValidationActionFilter> _logger;

        public ValidationActionFilter(IServiceProvider serviceProvider, ILogger<ValidationActionFilter> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Check if validation should be skipped
            if (ShouldSkipValidation(context))
            {
                return;
            }

            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument == null) continue;

                var argumentType = argument.GetType();
                
                // Skip primitive types and common framework types
                if (IsPrimitiveOrFrameworkType(argumentType))
                    continue;

                var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);
                
                var validator = _serviceProvider.GetService(validatorType) as IValidator;
                if (validator != null)
                {
                    _logger.LogDebug("Validating {ArgumentType} using FluentValidation", argumentType.Name);
                    
                    var validationContext = new ValidationContext<object>(argument);
                    var validationResult = validator.Validate(validationContext);

                    if (!validationResult.IsValid)
                    {
                        _logger.LogWarning("Validation failed for {ArgumentType}. Errors: {Errors}", 
                            argumentType.Name, 
                            string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

                        var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                        
                        var response = new BaseResponse
                        {
                            StatusCode = 400,
                            Message = "VALIDATION_ERROR",
                            Data = validationResult.Errors.Select(e => new { 
                                Field = e.PropertyName, 
                                Error = e.ErrorMessage 
                            }).ToList()
                        };

                        context.Result = new BadRequestObjectResult(response);
                        return;
                    }
                    else
                    {
                        _logger.LogDebug("Validation passed for {ArgumentType}", argumentType.Name);
                    }
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Nothing to do after action execution
        }

        private static bool ShouldSkipValidation(ActionExecutingContext context)
        {
            // Check for SkipValidation attribute on action
            if (context.ActionDescriptor.EndpointMetadata.OfType<SkipValidationAttribute>().Any())
                return true;

            // Check for SkipValidation attribute on controller
            var controllerType = context.Controller.GetType();
            if (controllerType.GetCustomAttributes(typeof(SkipValidationAttribute), true).Any())
                return true;

            return false;
        }

        private static bool IsPrimitiveOrFrameworkType(Type type)
        {
            return type.IsPrimitive 
                || type == typeof(string) 
                || type == typeof(DateTime) 
                || type == typeof(decimal) 
                || type == typeof(Guid)
                || type.IsEnum
                || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }
    }
}