using FluentValidation;

namespace Application;

public class BatchMoveItemCommandValidator : AbstractValidator<BatchMoveItemCommand>
{
    private const int MaxBatchSize = 50;
    public BatchMoveItemCommandValidator()
    {
        RuleFor(x => x.Spaces)
            .Must(x => x.Count <= MaxBatchSize)
            .WithMessage($"Cannot move more than {MaxBatchSize} spaces at once.");

        RuleFor(x => x.Folders)
            .Must(x => x.Count <= MaxBatchSize)
            .WithMessage($"Cannot move more than {MaxBatchSize} folders at once.");

        RuleFor(x => x.Tasks)
            .Must(x => x.Count <= MaxBatchSize)
            .WithMessage($"Cannot move more than {MaxBatchSize} tasks at once.");

        RuleFor(x => x)
            .Must(x => x.HasAnyMoves)
            .WithMessage("At least one item must be provided.");
    }
}