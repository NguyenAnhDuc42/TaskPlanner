using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.SpaceFeatures.SelfManagement.EditSpace;

public record class EditSpaceCommand(Guid spaceId, string name, string description, string color, string icon, bool isPrivate, bool isArchived) : ICommand<Unit>;
