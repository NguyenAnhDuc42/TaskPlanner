using FluentValidation;

namespace Application.Features.ChatRoomFeatures.CreateChatRoom;

public class CreateChatRoomValidator : AbstractValidator<CreateChatRoomCommand>
{
    public CreateChatRoomValidator()
    {
        RuleFor(x => x.name).NotEmpty();
    }
}
