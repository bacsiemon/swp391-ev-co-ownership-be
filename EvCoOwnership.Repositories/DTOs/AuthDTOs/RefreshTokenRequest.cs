using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvCoOwnership.DTOs.AuthDTOs
{
    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = null!;
    }

    public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
    {
        public RefreshTokenRequestValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("REFRESH_TOKEN_REQUIRED");
        }
    }
}