using FluentValidation;

namespace Application.Features.ViewFeatures;

public class DeleteViewValidator : AbstractValidator<DeleteViewCommand>
{
    public DeleteViewValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
