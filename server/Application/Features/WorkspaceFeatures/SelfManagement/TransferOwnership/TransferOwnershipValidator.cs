using FluentValidation;

namespace Application.Features.WorkspaceFeatures;

public class TransferOwnershipValidator : AbstractValidator<TransferOwnershipCommand>
{
    public TransferOwnershipValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty()
            .WithMessage("Workspace ID is required.");

        RuleFor(x => x.NewOwnerId)
            .NotEmpty()
            .WithMessage("New owner ID is required.");
    }
}
