using FluentValidation;

namespace Api;

public class UploadAttachmentValidator : AbstractValidator<UploadAttachmentCommand>
{
    private const long MaxSizeBytes = 25 * 1024 * 1024; // 25MB

    public UploadAttachmentValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("Content type is required.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("File is empty.")
            .Must(c => c.Length <= MaxSizeBytes).WithMessage("File exceeds the 25MB size limit.");
    }
}
