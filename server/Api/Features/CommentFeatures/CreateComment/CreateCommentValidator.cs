using FluentValidation;

namespace Api;

public class CreateCommentValidator : AbstractValidator<CreateCommentCommand>
{
    public CreateCommentValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Comment ID is required.");
        RuleFor(x => x.ProjectTaskId).NotEmpty().WithMessage("Task ID is required.");
        RuleFor(x => x.Content).NotEmpty().WithMessage("Comment content is required.");
    }
}
