using Domain.Common.Interfaces;
using Domain.Entities.Relationship;
using Domain.Entities.Support;
using Domain.Enums;


namespace Domain.Entities.ProjectEntities
{
    public class ProjectSpace : Aggregate, IHasWorkspaceId
    {
        public Guid ProjectWorkspaceId { get; private set; }
        public string Name { get; private set; } = null!;
        public string? Description { get; private set; }
        public string Icon { get; private set; } = null!;
        public string Color { get; private set; } = null!;
        public Visibility Visibility { get; private set; }
        public bool IsArchived { get; private set; }
        public long? OrderKey { get; private set; }
        public Guid CreatorId { get; private set; }
        public Guid WorkspaceId => ProjectWorkspaceId;

        // === Collections ===
        private readonly List<Status> _statuses = new();
        public IReadOnlyCollection<Status> Statuses => _statuses.AsReadOnly();

        private readonly List<UserProjectSpace> _members = new();
        public IReadOnlyCollection<UserProjectSpace> Members => _members.AsReadOnly();

        private ProjectSpace() { } // For EF Core

        internal ProjectSpace(Guid id, Guid workspaceId, string name, string? description,
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

        // === MEMBERSHIP MANAGEMENT ===

        public void AddMember(Guid userId)
        {
            ValidateGuid(userId, nameof(userId));

            if (_members.Any(m => m.UserId == userId))
                throw new InvalidOperationException("User is already a member of this space.");

            var member = UserProjectSpace.Create(userId, Id);
            _members.Add(member);
            UpdateTimestamp();
        }

        public void RemoveMember(Guid userId)
        {
            if (userId == CreatorId)
                throw new InvalidOperationException("Cannot remove space creator.");

            var member = _members.FirstOrDefault(m => m.UserId == userId);
            if (member == null)
                throw new InvalidOperationException("User is not a member of this space.");

            _members.Remove(member);
            UpdateTimestamp();
        }

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

        private static void ValidateGuid(Guid id, string paramName)
        {
            if (id == Guid.Empty) throw new ArgumentException("Guid cannot be empty.", paramName);
        }
    }
}