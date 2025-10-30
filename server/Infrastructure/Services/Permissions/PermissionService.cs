using Domain.Common;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Entities.Support.Workspace;
using Infrastructure.Data;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Application.Interfaces.Services.Permissions;
using Domain.Common.Interfaces;
using Domain;

namespace Infrastructure.Services.Permissions
{
    public class PermissionService : IPermissionService
    {
        private readonly PermissionContextBuilder _builder;
        private readonly WorkspaceContext _workspaceContext;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(
            PermissionContextBuilder builder,
            ILogger<PermissionService> logger,
            WorkspaceContext workspaceContext)
        {
            _builder = builder;
            _logger = logger;
            _workspaceContext = workspaceContext;
        }

        public async Task<bool> HasPermissionAsync<TEntity>(Guid userId, TEntity entity, PermissionAction action, CancellationToken ct) where TEntity : class
        {
            var workspaceId = _workspaceContext.WorkspaceId;

            var context = await _builder.BuildFromEntityAsync(userId, workspaceId, entity, ct);

            var result = PermissionMatrix.CanPerform(entityType, action, context);

            if (!result)
            {
                _logger.LogWarning(
                    "Permission denied: User {UserId} attempted {Action} on {EntityType} {EntityId}",
                    userId, action, entityType, entity.Id);
            }

            return result;
        }

        public Task<bool> CanPerformInScopeAsync(Guid userId, Guid? scopeId, EntityType scopeType, PermissionAction action, CancellationToken cancellationToken = default)
        {
            var workspaceId = _workspaceContext.WorkspaceId;
            var context = await _builder.BuildScopeContextAsync(userId, workspaceId, entityId, entityType, cancellationToken);

            var result = PermissionMatrix.CanPerform(entityType, action, context);

            if (!result)
            {
                _logger.LogWarning(
                    "Permission denied: User {UserId} attempted {Action} on {EntityType} {EntityId}",
                    userId, action, entityType, entityId);
            }

            return result;
        }

        // private static EntityType GetEntityType<TEntity>() where TEntity : class =>
        //     typeof(TEntity).Name switch
        //     {
        //         nameof(ChatRoom) => EntityType.ChatRoom,
        //         nameof(ChatMessage) => EntityType.ChatMessage,
        //         nameof(ProjectTask) => EntityType.ProjectTask,
        //         nameof(ProjectList) => EntityType.ProjectList,
        //         nameof(ProjectFolder) => EntityType.ProjectFolder,
        //         nameof(ProjectSpace) => EntityType.ProjectSpace,
        //         nameof(ProjectWorkspace) => EntityType.ProjectWorkspace,
        //         nameof(WorkspaceMember) => EntityType.WorkspaceMember,
        //         nameof(ChatRoomMember) => EntityType.ChatRoomMember,
        //         _ => throw new InvalidOperationException($"Unknown entity type: {typeof(TEntity).Name}")
        //     };


    }
}