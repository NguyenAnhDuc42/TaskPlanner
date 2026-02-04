using FluentValidation;

namespace Application.Features.ListFeatures.SelfManagement.CreateList;

public class CreateListValidator : AbstractValidator<CreateListCommand>
{
    public CreateListValidator()
    {
        RuleFor(x => x.name).NotEmpty();
    }
}
