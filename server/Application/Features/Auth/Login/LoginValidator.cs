using System;
using FluentValidation;

namespace Application.Features.Auth.Login;

public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
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
