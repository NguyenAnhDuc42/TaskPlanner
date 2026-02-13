using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.SpaceFeatures.SelfManagement.DeleteSpace;

public record class DeleteSpaceCommand(Guid SpaceId) : ICommand<Unit>;
