using System;
using FluentValidation;

namespace src.Feature.User.Auth.Login;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress() // Add this for email format validation
            .WithMessage("Invalid email format."); // Custom message for email format

        RuleFor(x => x.password)
            .NotEmpty() // Change from Empty() to NotEmpty()
            .WithMessage("Password is required.");
    }
}
