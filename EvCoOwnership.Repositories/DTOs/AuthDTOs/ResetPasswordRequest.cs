using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvCoOwnership.Repositories.DTOs.AuthDTOs
{
    public class ResetPasswordRequest
    {
        public string Email { get; set; }
        public string Otp { get; set; }
        public string NewPassword { get; set; }
    }

    public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
    {
        public ResetPasswordRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("EMAIL_REQUIRED")
                .EmailAddress().WithMessage("INVALID_EMAIL_FORMAT");

            RuleFor(x => x.Otp)
                .Length(6).WithMessage("OTP_MIN_6_CHARACTERS");

            RuleFor(x => x.NewPassword)
                .MinimumLength(8).WithMessage("NEW_PASSWORD_MIN_8_CHARACTERS");
        }
    }
}
