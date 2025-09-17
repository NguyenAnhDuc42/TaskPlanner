using System.ComponentModel.DataAnnotations;
using Domain.Common;
using Domain.Common.Interfaces;
using Domain.Entities.Relationship;
using Domain.Entities.Support;
using Domain.Enums;


namespace Domain.Entities.ProjectEntities
{
    public class ProjectSpace : Entity
    {
        [Required] public Guid ProjectWorkspaceId { get; private set; }
        [Required] public string Name { get; private set; } = null!;
        public string? Description { get; private set; }
        public string Icon { get; private set; } = null!;
        public string Color { get; private set; } = "#cdcbcbff";
        public Visibility Visibility { get; private set; }
        public bool IsArchived { get; private set; }
        public long? OrderKey { get; private set; }
        [Required] public Guid CreatorId { get; private set; }

        private ProjectSpace() { } // For EF Core

        private ProjectSpace(Guid id, Guid workspaceId, string name, string? description,
            string icon, string color, Visibility visibility, long orderKey, Guid creatorId)
        {
            Id = id;
            ProjectWorkspaceId = workspaceId;
            Name = name;
            Description = description;
            Icon = icon;
            Color = color;
            Visibility = visibility;
            OrderKey = orderKey;
            CreatorId = creatorId;
            IsArchived = false;
        }

        public static ProjectSpace Create(Guid workspaceId, string name, string? description, string color, string icon, Visibility visibility, Guid creatorId, long orderKey)
        {
            name = name?.Trim() ?? string.Empty;
            description = string.IsNullOrWhiteSpace(description?.Trim()) ? null : description.Trim();
            color = color?.Trim() ?? string.Empty;
            icon = icon?.Trim() ?? string.Empty;

            ValidateBasicInfo(name, description);
            ValidateVisualSettings(color, icon);

            var space = new ProjectSpace(Guid.NewGuid(), workspaceId, name, description, icon, color, visibility, orderKey, creatorId);
            return space;
        }

        // === SELF MANAGEMENT ===

        public void UpdateBasicInfo(string name, string? description)
        {
            name = name?.Trim() ?? string.Empty;
            description = string.IsNullOrWhiteSpace(description?.Trim()) ? null : description.Trim();

            if (Name == name && Description == description) return;

            ValidateBasicInfo(name, description);

            Name = name;
            Description = description;
            UpdateTimestamp();
        }

        public void UpdateVisualSettings(string color, string icon)
        {
            color = color?.Trim() ?? string.Empty;
            icon = icon?.Trim() ?? string.Empty;

            if (Color == color && Icon == icon) return;

            ValidateVisualSettings(color, icon);

            Color = color;
            Icon = icon;
            UpdateTimestamp();
        }

        public void ChangeVisibility(Visibility newVisibility)
        {
            if (Visibility == newVisibility) return;

            Visibility = newVisibility;
            UpdateTimestamp();
        }

        public void Archive()
        {
            if (IsArchived) return;

            IsArchived = true;
            UpdateTimestamp();
        }

        public void Unarchive()
        {
            if (!IsArchived) return;

            IsArchived = false;
            UpdateTimestamp();
        }

        public void UpdateOrderKey(long newKey) => OrderKey = newKey;

        // === VALIDATION HELPERS ===

        private static void ValidateBasicInfo(string name, string? description)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Space name cannot be empty.", nameof(name));
            if (name.Length > 100)
                throw new ArgumentException("Space name cannot exceed 100 characters.", nameof(name));
            if (description?.Length > 500)
                throw new ArgumentException("Space description cannot exceed 500 characters.", nameof(description));
        }

        private static void ValidateVisualSettings(string color, string icon)
        {
            if (string.IsNullOrWhiteSpace(icon))
                throw new ArgumentException("Space icon cannot be empty.", nameof(icon));
            if (string.IsNullOrWhiteSpace(color))
                throw new ArgumentException("Space color cannot be empty.", nameof(color));
        }

    }
}