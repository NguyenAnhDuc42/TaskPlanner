using FluentValidation;

namespace Application.Features.Auth.OAuth;

public class ExternalLoginValidator : AbstractValidator<ExternalLoginCommand>
{
    public ExternalLoginValidator()
    {
        RuleFor(x => x.Provider).NotEmpty().WithMessage("Provider is required.");
        RuleFor(x => x.Token).NotEmpty().WithMessage("Token is required.");
    }
}
