using FluentValidation;

namespace Api;

public class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Task ID is required.");
        RuleFor(x => x.ProjectWorkspaceId).NotEmpty().WithMessage("Workspace ID is required.");
        RuleFor(x => x.ProjectSpaceId).NotEmpty().WithMessage("Space ID is required — tasks must belong to a space.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Task name is required.");
        RuleFor(x => x.Slug).NotEmpty().WithMessage("Task slug is required.");
        RuleFor(x => x.DefaultDocumentId).NotEmpty().WithMessage("Default document ID is required.");
        RuleFor(x => x.OrderKey).NotEmpty().WithMessage("Order key is required.");
    }
}
