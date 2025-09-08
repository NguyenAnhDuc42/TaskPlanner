using Application.Contract.SpaceContract;
using Domain.Enums;
using MediatR;

namespace Application.Features.SpaceFeatures.CreateSpace;

public record class CreateSpaceCommand(Guid workspaceId,string name,string? description,string color,string icon,long orderKey,Visibility? visibility = Visibility.Public) : IRequest<SpaceSummary>;
