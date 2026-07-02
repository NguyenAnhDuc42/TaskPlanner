using FluentValidation;

namespace Api;

public class UpdateCommentValidator : AbstractValidator<UpdateCommentCommand>
{
    public UpdateCommentValidator()
    {
        RuleFor(x => x.CommentId).NotEmpty().WithMessage("Comment ID is required.");
        RuleFor(x => x.Content).NotEmpty().WithMessage("Comment content is required.");
    }
}
