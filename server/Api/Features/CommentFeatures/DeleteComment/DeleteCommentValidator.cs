using FluentValidation;

namespace Api;

public class DeleteCommentValidator : AbstractValidator<DeleteCommentCommand>
{
    public DeleteCommentValidator()
    {
        RuleFor(x => x.CommentId).NotEmpty().WithMessage("Comment ID is required.");
    }
}
