using System;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Errors.Model;

namespace Infrastructure.Services;

public class PermissionService : IPermissionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly HybridCache _cache;
    private readonly ILogger<PermissionService> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public PermissionService(IUnitOfWork unitOfWork, HybridCache cache, ILogger<PermissionService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<T> GetEntityWithPermissionAsync<T>(Guid entityId, Guid userId, Permission requiredPermission, Func<IQueryable<T>, IQueryable<T>>? includeFunc = null, CancellationToken ct = default) where T : class
    {
        var query = _unitOfWork.Set<T>().AsQueryable();
        
        if (includeFunc != null) query = includeFunc(query);
        var entity = await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == entityId, ct)
            ?? throw new NotFoundException($"{typeof(T).Name} {entityId} not found");
        await EnsurePermissionAsync(userId, GetWorkspaceId(entity), requiredPermission, ct);

        return entity;

    }
    public Task EnsurePermissionAsync(Guid userId, Guid workspaceId, Permission permission, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task EnsurePermissionAsync(Guid userId, Guid workspaceId, Permission[] permissions, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Guid>> GetUserAccessibleWorkspacesAsync(Guid userId, Permission permission, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Permission> GetUserPermissionsAsync(Guid userId, Guid workspaceId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Role?> GetUserRoleAsync(Guid userId, Guid workspaceId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> HasPermissionAsync(Guid userId, Guid workspaceId, Permission permission, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> HasPermissionAsync(Guid userId, Guid workspaceId, Permission[] permissions, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsWorkspaceCreatorAsync(Guid userId, Guid workspaceId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsWorkspaceOwnerAsync(Guid userId, Guid workspaceId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    private static Permission GetRolePermissions(Role role) => role switch
    {
        Role.Owner => Permission.Owner_Permissions,

        Role.Admin => Permission.Workspace_Admin | Permission.Member_Admin | Permission.Content_Admin |
                     Permission.View_Reports | Permission.Export_Data |
                     Permission.View_Comments | Permission.Create_Comments | Permission.Edit_All_Comments | Permission.Delete_All_Comments |
                     Permission.View_Attachments | Permission.Upload_Attachments | Permission.Delete_All_Attachments,

        Role.Member => Permission.View_Workspace | Permission.View_Members | Permission.View_Spaces |
                      Permission.Create_Spaces | Permission.Edit_Spaces | Permission.Archive_Spaces |
                      Permission.View_Lists | Permission.Create_Lists | Permission.Edit_Lists | Permission.Reorder_Lists |
                      Permission.View_Tasks | Permission.Create_Tasks | Permission.Edit_Tasks | Permission.Assign_Tasks | Permission.Change_Task_Status |
                      Permission.View_Statuses | Permission.Create_Statuses | Permission.Edit_Statuses |
                      Permission.View_Comments | Permission.Create_Comments | Permission.Edit_Own_Comments | Permission.Delete_Own_Comments |
                      Permission.View_Attachments | Permission.Upload_Attachments | Permission.Delete_Own_Attachments,

        Role.Guest => Permission.View_Workspace | Permission.View_Members | Permission.View_Spaces |
                      Permission.View_Lists | Permission.View_Tasks | Permission.View_Statuses |
                      Permission.View_Comments | Permission.View_Attachments,

        _ => Permission.None
    };
}
