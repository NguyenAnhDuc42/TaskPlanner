using FluentValidation;

namespace Api;

public class DeleteSpaceValidator : AbstractValidator<DeleteSpaceCommand>
{
    public DeleteSpaceValidator()
    {
        RuleFor(x => x.SpaceId).NotEmpty().WithMessage("Space ID is required.");
    }
}
