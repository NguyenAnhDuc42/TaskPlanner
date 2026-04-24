using FluentValidation;
using Domain.Enums;
using Domain.Enums.RelationShip;

namespace Application.Features.WorkspaceFeatures;

public class UpdateMembersValidator : AbstractValidator<UpdateMembersCommand>
{
    public UpdateMembersValidator()
    {
        RuleFor(x => x.workspaceId).NotEmpty();
        RuleFor(x => x.members).NotEmpty().WithMessage("At least one member must be provided.");
        RuleForEach(x => x.members).SetValidator(new UpdateMemberValueValidator());
    }
}

public class UpdateMemberValueValidator : AbstractValidator<UpdateMemberValue>
{
    public UpdateMemberValueValidator()
    {
        RuleFor(x => x.userId).NotEmpty();
        RuleFor(x => x.role)
            .Must(r => r == null || Enum.IsDefined(typeof(Role), r))
            .WithMessage("Invalid role provided.");
        RuleFor(x => x.status)
            .Must(s => s == null || Enum.IsDefined(typeof(MembershipStatus), s))
            .WithMessage("Invalid status provided.");
    }
}
