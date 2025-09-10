using Application.Contract.WorkspaceContract;
using Domain.Enums;
using MediatR;

namespace Application.Features.WorkspaceFeatures.CreateWrokspace;

public record class CreateWorkspaceCommand(string name, string? description, string color, string icon, Guid creatorId, Visibility visibility) : IRequest<WorkspaceDetail>;

