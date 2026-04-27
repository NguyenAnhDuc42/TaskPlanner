using Domain.Common;
using Domain.Enums;
using Domain.Enums.RelationShip;
using System.Collections.Generic;

namespace Domain.Entities;

public class ViewDefinition : TenantEntity
{
    public Guid? ProjectSpaceId { get; private set; }
    public Guid? ProjectFolderId { get; private set; }
    public string Name { get; private set; } = null!;
    public ViewType ViewType { get; private set; }
    public bool IsDefault { get; private set; }
    public string SortOrder { get; private set; } = null!;
    
    // Shared configurations stored as JSON in DB but typed in Domain
    public ViewFilterConfig FilterConfig { get; private set; } = ViewFilterConfig.CreateDefault();
    public string? DisplayConfigJson { get; private set; } // TODO: Make this type-safe later

    private ViewDefinition() { } // EF Core

    private ViewDefinition(
        Guid id,
        Guid projectWorkspaceId,
        Guid? projectSpaceId,
        Guid? projectFolderId,
        string name,
        ViewType viewType,
        bool isDefault,
        string sortOrder,
        Guid creatorId)
        : base(id, projectWorkspaceId)
    {
        ProjectSpaceId = projectSpaceId;
        ProjectFolderId = projectFolderId;
        Name = name;
        ViewType = viewType;
        IsDefault = isDefault;
        SortOrder = sortOrder;
        
        // Audit is initialized in base constructor
        InitializeAudit(creatorId);
    }

    public static ViewDefinition Create(
        Guid projectWorkspaceId,
        Guid? projectSpaceId,
        Guid? projectFolderId,
        string name,
        ViewType viewType,
        Guid creatorId,
        bool isDefault = false,
        string? sortOrder = null)
    {
        return new ViewDefinition(Guid.NewGuid(), projectWorkspaceId, projectSpaceId, projectFolderId, name, viewType, isDefault, sortOrder ?? FractionalIndex.Start(), creatorId);
    }

    public static List<ViewDefinition> CreateDefaults(Guid projectWorkspaceId, Guid? projectSpaceId, Guid? projectFolderId, Guid creatorId)
    {
        var start = FractionalIndex.Start();
        return new List<ViewDefinition>
        {
            Create(projectWorkspaceId, projectSpaceId, projectFolderId, "Overview", ViewType.Overview, creatorId, isDefault: true, sortOrder: start),
            Create(projectWorkspaceId, projectSpaceId, projectFolderId, "Tasks", ViewType.Tasks, creatorId, isDefault: false, sortOrder: FractionalIndex.After(start))
        };
    }

    public void UpdateName(string name)
    {
        Name = name;
        UpdateTimestamp();
    }

    public void UpdateFilterConfig(ViewFilterConfig filterConfig)
    {
        if (FilterConfig == filterConfig) return;
        FilterConfig = filterConfig;
        UpdateTimestamp();
    }

    public void UpdateDisplayConfig(string? displayConfigJson)
    {
        if (DisplayConfigJson == displayConfigJson) return;
        DisplayConfigJson = displayConfigJson;
        UpdateTimestamp();
    }

    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
        UpdateTimestamp();
    }
}
