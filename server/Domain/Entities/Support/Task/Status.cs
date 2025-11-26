using System;
using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Support;

public class Status : Entity
{
    public Guid? LayerId { get; private set; }
    public EntityLayerType LayerType { get; private set; }
    public string Name { get; private set; } = null!;
    public string Color { get; private set; } = null!;
    public StatusCategory Category { get; private set; }
    public long OrderKey { get; private set; }
    public bool IsDefaultStatus { get; private set; }

    private Status() { } // EF Core

    private Status(Guid id, Guid? layerId, EntityLayerType layerType, string name, string color, StatusCategory category, long orderKey)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Status name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(color)) throw new ArgumentException("Status color cannot be empty.", nameof(color));
        if (layerId == Guid.Empty) throw new ArgumentException(nameof(layerId));
        if (layerType == EntityLayerType.None) throw new ArgumentException(nameof(layerType));

        Name = name.Trim();
        Color = color.Trim();
        Category = category;
        LayerId = layerId;
        LayerType = layerType;
        OrderKey = orderKey;
        IsDefaultStatus = false;
    }

    public static Status Create(Guid layerId, EntityLayerType layerType, string name, string color, StatusCategory category, long orderKey)
        => new Status(Guid.NewGuid(), layerId, layerType, name, color, category, orderKey);

    public void UpdateDetails(string newName, string newColor, StatusCategory? newCategory = null)
    {
        if (string.IsNullOrWhiteSpace(newName)) throw new ArgumentException("Status name cannot be empty.", nameof(newName));
        if (string.IsNullOrWhiteSpace(newColor)) throw new ArgumentException("Status color cannot be empty.", nameof(newColor));
        
        var changed = false;
        if (Name != newName.Trim()) { Name = newName.Trim(); changed = true; }
        if (Color != newColor.Trim()) { Color = newColor.Trim(); changed = true; }
        if (newCategory.HasValue && Category != newCategory.Value) { Category = newCategory.Value; changed = true; }
        
        if (changed) UpdateTimestamp();
    }

    public void UpdateOrderKey(long newKey)
    {
        if (newKey < 0) throw new ArgumentOutOfRangeException(nameof(newKey));
        if (OrderKey == newKey) return;

        OrderKey = newKey;
        UpdateTimestamp();
    }

    public void SetDefault(bool isDefault)
    {
        if (IsDefaultStatus == isDefault) return;
        IsDefaultStatus = isDefault;
        UpdateTimestamp();
    }

    public void SetLayer(Guid layerId, EntityLayerType layerType)
    {
        if (LayerId == layerId && LayerType == layerType) return;
        LayerId = layerId;
        LayerType = layerType;
        UpdateTimestamp();
    }
}
