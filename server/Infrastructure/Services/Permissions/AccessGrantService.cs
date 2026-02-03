using Application.Interfaces.Services.Permissions;
using Domain.Entities.Relationship;
using Domain.Enums.RelationShip;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Infrastructure.Services.Permissions;

public sealed class AccessGrantService : IAccessGrantService
{
    private readonly TaskPlanDbContext _context;
    private readonly HybridCache _cache;

    public AccessGrantService(TaskPlanDbContext context, HybridCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task GrantAccess(
        Guid workspaceMemberId,
        EntityLayerType entityLayer,
        Guid entityId,
        AccessLevel accessLevel,
        Guid grantedByUserId,
        CancellationToken ct)
    {
        // Validate member exists and get their workspace
        var member = await _context.WorkspaceMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == workspaceMemberId, ct)
            ?? throw new InvalidOperationException($"Workspace member {workspaceMemberId} not found");

        // Validate entity workspace matches member.ProjectWorkspaceId
        var entityWorkspaceId = await GetEntityWorkspaceId(entityLayer, entityId, ct);
        if (entityWorkspaceId != member.ProjectWorkspaceId)
            throw new InvalidOperationException("Entity does not belong to the same workspace as the member");

        // Upsert pattern
        var existing = await _context.EntityAccesses
            .FirstOrDefaultAsync(
                x => x.WorkspaceMemberId == workspaceMemberId
                  && x.EntityLayer == entityLayer
                  && x.EntityId == entityId,
                cancellationToken: ct);

        if (existing != null)
        {
            existing.Update(accessLevel, grantedByUserId);
            _context.EntityAccesses.Update(existing);
        }
        else
        {
            var access = EntityAccess.Create(workspaceMemberId, entityId, entityLayer, accessLevel, grantedByUserId);
            _context.EntityAccesses.Add(access);
        }

        await _context.SaveChangesAsync(ct);

        // Invalidate membership cache
        await _cache.RemoveAsync($"wsm:{member.UserId}:{member.ProjectWorkspaceId}", cancellationToken: ct);
    }

    public async Task RevokeAccess(
        Guid workspaceMemberId,
        EntityLayerType entityLayer,
        Guid entityId,
        CancellationToken ct)
    {
        var existing = await _context.EntityAccesses
            .FirstOrDefaultAsync(
                x => x.WorkspaceMemberId == workspaceMemberId
                  && x.EntityLayer == entityLayer
                  && x.EntityId == entityId,
                cancellationToken: ct);

        if (existing == null)
            return;

        _context.EntityAccesses.Remove(existing);
        await _context.SaveChangesAsync(ct);

        var member = await _context.WorkspaceMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == workspaceMemberId, ct);

        if (member != null)
        {
            await _cache.RemoveAsync($"wsm:{member.UserId}:{member.ProjectWorkspaceId}", cancellationToken: ct);
        }
    }

    private async Task<Guid> GetEntityWorkspaceId(EntityLayerType entityLayer, Guid entityId, CancellationToken ct)
    {
        return entityLayer switch
        {
            EntityLayerType.ProjectSpace => await _context.ProjectSpaces.Where(x => x.Id == entityId).Select(x => x.ProjectWorkspaceId).FirstOrDefaultAsync(ct),
            EntityLayerType.ProjectFolder => await _context.ProjectFolders.Where(x => x.Id == entityId).Select(x => x.ProjectSpaceId).Join(_context.ProjectSpaces, fId => fId, s => s.Id, (fId, s) => s.ProjectWorkspaceId).FirstOrDefaultAsync(ct),
            EntityLayerType.ProjectList => await _context.ProjectLists.Where(x => x.Id == entityId).Select(x => x.ProjectSpaceId).Join(_context.ProjectSpaces, lId => lId, s => s.Id, (lId, s) => s.ProjectWorkspaceId).FirstOrDefaultAsync(ct),
            EntityLayerType.ProjectTask => await _context.ProjectTasks.Where(x => x.Id == entityId).Select(x => x.ProjectListId).Join(_context.ProjectLists, tId => tId, l => l.Id, (tId, l) => l.ProjectSpaceId).Join(_context.ProjectSpaces, lSId => lSId, s => s.Id, (lSId, s) => s.ProjectWorkspaceId).FirstOrDefaultAsync(ct),
            EntityLayerType.ChatRoom => await _context.ChatRooms.Where(x => x.Id == entityId).Select(x => x.ProjectWorkspaceId).FirstOrDefaultAsync(ct),
            _ => throw new InvalidOperationException("Unsupported entity layer")
        };
    }
}
