using Domain.Common;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Application.Interfaces.Services.Permissions;
using Application.Common;

namespace Infrastructure.Services.Permissions;

public class PermissionService : IPermissionService
{
    private readonly PermissionContextBuilder _contextBuilder;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        PermissionContextBuilder contextBuilder,
        ILogger<PermissionService> logger)
    {
        _contextBuilder = contextBuilder;
        _logger = logger;
    }

    // Variant 1: Action on entity itself
    public async Task<bool> CanPerformAsync<TEntity>(
        Guid workspaceId,
        Guid userId,
        TEntity entity,
        PermissionAction action,
        CancellationToken ct) where TEntity : Entity
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        ct.ThrowIfCancellationRequested();

        var entityType = PermissionDataFetcher.GetEntityType<TEntity>();

        // Get rule to check what data it needs
        var rule = PermissionMatrix.GetRule(entityType, action);
        if (rule == null)
        {
            _logger.LogWarning("No permission rule found for {EntityType}.{Action}", entityType, action);
            return false;
        }

        // Build context with selective fetching
        var context = await _contextBuilder.BuildForEntityAsync(
            workspaceId, userId, entity, rule.DataNeeds, ct);

        // Evaluate rule
        var result = rule.Evaluate(context);
        if (!result)
        {
            _logger.LogWarning("Permission denied: User {UserId} action {Action} on {EntityType} {EntityId}",
                userId, action, entityType, entity.Id);
        }

        return result;
    }

    // Variant 2: Create child in parent
    public async Task<bool> CanPerformAsync<TParent>(
        Guid workspaceId,
        Guid userId,
        TParent parentEntity,
        EntityType childType,
        PermissionAction action,
        CancellationToken ct) where TParent : Entity
    {
        if (parentEntity == null) throw new ArgumentNullException(nameof(parentEntity));
        ct.ThrowIfCancellationRequested();

        // Get rule for CHILD type (what we're creating)
        var rule = PermissionMatrix.GetRule(childType, action);
        if (rule == null)
        {
            _logger.LogWarning("No permission rule found for {EntityType}.{Action}", childType, action);
            return false;
        }

        // Build context from parent with selective fetching
        var context = await _contextBuilder.BuildForParentChildAsync(
            workspaceId, userId, parentEntity, rule.DataNeeds, ct);

        // Evaluate child rule using parent context
        var result = rule.Evaluate(context);
        if (!result)
        {
            var parentType = PermissionDataFetcher.GetEntityType<TParent>();
            _logger.LogWarning("Permission denied: User {UserId} create {ChildType} in {ParentType} {ParentId}",
                userId, childType, parentType, parentEntity.Id);
        }

        return result;
    }

    // Variant 3: Action on child with parent context
    public async Task<bool> CanPerformAsync<TChild, TParent>(
        Guid workspaceId,
        Guid userId,
        TChild childEntity,
        TParent parentEntity,
        PermissionAction action,
        CancellationToken ct)
        where TChild : Entity
        where TParent : Entity
    {
        if (parentEntity == null) throw new ArgumentNullException(nameof(parentEntity));
        if (childEntity == null) throw new ArgumentNullException(nameof(childEntity));
        ct.ThrowIfCancellationRequested();

        var childType = PermissionDataFetcher.GetEntityType<TChild>();

        // Get rule for child
        var rule = PermissionMatrix.GetRule(childType, action);
        if (rule == null)
        {
            _logger.LogWarning("No permission rule found for {EntityType}.{Action}", childType, action);
            return false;
        }

        // Build context with selective fetching
        var context = await _contextBuilder.BuildForChildWithParentAsync(
            workspaceId, userId, childEntity, parentEntity, rule.DataNeeds, ct);

        // Evaluate rule
        var result = rule.Evaluate(context);
        if (!result)
        {
            _logger.LogWarning("Permission denied: User {UserId} action {Action} on {EntityType} {EntityId}",
                userId, action, childType, childEntity.Id);
        }

        return result;
    }
}