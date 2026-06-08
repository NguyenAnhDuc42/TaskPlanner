using FluentValidation;
namespace Application;

public class UpdateMembersValidator : AbstractValidator<UpdateMembersCommand>
{
    public UpdateMembersValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Members).NotEmpty().WithMessage("At least one member must be provided.");
        RuleForEach(x => x.Members).SetValidator(new UpdateMemberValueValidator());
    }
}

public class UpdateMemberValueValidator : AbstractValidator<UpdateMemberValue>
{
    public UpdateMemberValueValidator()
    {
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.Role)
            .Must(r => r == null || Enum.IsDefined(typeof(Role), r))
            .WithMessage("Invalid role provided.");
        RuleFor(x => x.Status)
            .Must(s => s == null || Enum.IsDefined(typeof(MembershipStatus), s))
            .WithMessage("Invalid status provided.");
    }
}


