using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.SpaceFeatures.SelfManagement.DeleteSpace;

public record class DeleteSpaceCommand(Guid spaceId) : ICommand<Unit>;
