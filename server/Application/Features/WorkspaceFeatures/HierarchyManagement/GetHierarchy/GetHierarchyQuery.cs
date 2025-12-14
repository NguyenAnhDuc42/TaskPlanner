using Application.Contract.WorkspaceContract;
using MediatR;

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.GetHierarchy;

public record class GetHierarchyQuery(Guid WorkspaceId) : IRequest<WorkspaceHierarchyDto>;
