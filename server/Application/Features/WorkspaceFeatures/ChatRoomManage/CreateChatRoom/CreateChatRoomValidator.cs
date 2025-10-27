using System;
using FluentValidation;

namespace Application.Features.WorkspaceFeatures.ChatRoomManage.CreateChatRoom;

public class CreateChatRoomValidator : AbstractValidator<CreateChatRoomCommand>
{
    public CreateChatRoomValidator()
    {
        RuleFor(x => x.name)
            .NotEmpty().WithMessage("Chat room name is required.")
            .MaximumLength(100).WithMessage("Chat room name must be at most 100 characters.");

        RuleFor(x => x.workspaceId)
            .NotEmpty().WithMessage("Workspace ID is required.");
    }
}
