using Application.Contract.WorkspaceContract;
using MediatR;

namespace Application.Features.WorkspaceFeatures.SelfMange.UpdateWorkspaceBasicInfo;

public record class UpdateWorkspaceBasicInfoCommand(Guid workspaceId,string? name,string? description) : IRequest<WorkspaceDetail>;


