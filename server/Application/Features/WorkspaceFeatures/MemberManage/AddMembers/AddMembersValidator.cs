using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.WorkspaceFeatures.MemberManage.AddMembers;

public class AddMembersValidator : AbstractValidator<AddMembersCommand>
{
    public AddMembersValidator()
    {
        RuleFor(x => x.workspaceId)
            .NotEmpty().WithMessage("WorkspaceId is required.");
        RuleFor(x => x.members)
            .NotEmpty().WithMessage("At least one member must be added.")
            .Must(members => members != null && members.Count > 0)
            .WithMessage("Members list cannot be empty.")
            .Must(members => members != null && members.Count <= 100)
            .WithMessage("Cannot add more than 100 members at once.");
        RuleForEach(x => x.members).ChildRules(member =>
        {
            member.RuleFor(m => m.email)
                .NotEmpty().WithMessage("Email is required for each member.");
            member.RuleFor(m => m.role)
                .IsInEnum().WithMessage("Role must be a valid enum value.");
        });
    }
}