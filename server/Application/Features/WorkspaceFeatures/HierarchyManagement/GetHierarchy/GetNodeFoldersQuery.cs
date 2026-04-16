using Application.Common.Interfaces;

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.GetHierarchy;

public record GetNodeFoldersQuery(Guid WorkspaceId, Guid NodeId) : IQueryRequest<List<FolderHierarchyDto>>, IAuthorizedWorkspaceRequest;
