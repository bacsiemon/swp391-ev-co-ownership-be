using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvCoOwnership.Repositories.DTOs.AuthDTOs
{
    public class LoginRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("EMAIL_REQUIRED")
                .EmailAddress().WithMessage("INVALID_EMAIL_FORMAT");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("PASSWORD_REQUIRED")
                .MinimumLength(8).WithMessage("PASSWORD_MIN_8_CHARACTERS");
        }
    }
}