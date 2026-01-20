using FluentValidation;


namespace Application.Features.WorkspaceFeatures.MemberManage.UpdateMembersRole;

public class UpdateMembersValidator : AbstractValidator<UpdateMembersCommand>
{
    public UpdateMembersValidator()
    {
        RuleFor(x => x.workspaceId)
            .NotEmpty().WithMessage("WorkspaceId is required.");
        RuleFor(x => x.members)
            .NotEmpty().WithMessage("At least one member must be added.")
            .Must(members => members != null && members.Count > 0)
            .WithMessage("Members list cannot be empty.")
            .Must(members => members != null && members.Count <= 100)
            .WithMessage("Cannot update more than 100 members at once.");
        RuleForEach(x => x.members).ChildRules(member =>
        {
            member.RuleFor(m => m.userId)
                .NotEmpty().WithMessage("UserId is required for each member.");
            member.RuleFor(m => m.role)
                .IsInEnum().WithMessage("Role must be a valid enum value.")
            member.RuleFor(m => m.status)
                .IsInEnum().WithMessage("Status must be a valid enum value.");
        });
    }
}
