using EvCoOwnership.Helpers.BaseClasses;
using FluentValidation;
using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvCoOwnership.Helpers.Helpers
{
    public static class ValidationHelper
    {
        public static BaseResponse? ValidateRequest<T>(this IValidator<T> validator, T request)
        { 
            ValidationResult? validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return new BaseResponse
                {
                    StatusCode = 400,
                    Message = "VALIDATION_FAILED",
                    Data = errors
                };
            }
            return null;
        }
    }
}
