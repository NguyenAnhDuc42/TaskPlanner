using Application.Contract.WorkspaceContract;
using MediatR;

namespace Application.Features.WorkspaceFeatures.SelfManagement.GetDetailWorkspace;

public record class GetDetailWorkspaceQuery(Guid WorkspaceId) : IRequest<WorkspaceDetailDto>;