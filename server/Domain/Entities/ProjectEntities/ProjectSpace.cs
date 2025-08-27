using Domain.Common;
using Domain.Entities.Relationship;
using Domain.Entities.Support;
using Domain.Enums;
using Domain.Events.SpaceEvents;
using Domain.Services.UsageChecker;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Entities.ProjectEntities
{
    public class ProjectSpace : Aggregate
    {
        private const int MAX_BATCH_SIZE = 1000; // Safety limit for BATCH_RULE operations

        public Guid ProjectWorkspaceId { get; private set; }
        public string Name { get; private set; } = null!;
        public string? Description { get; private set; }
        public string Icon { get; private set; } = null!;
        public string Color { get; private set; } = null!;
        public Visibility Visibility { get; private set; }
        public bool IsArchived { get; private set; }
        public int? OrderIndex { get; private set; }
        public Guid CreatorId { get; private set; }

        private readonly List<UserProjectSpace> _members = new();
        public IReadOnlyCollection<UserProjectSpace> Members => _members.AsReadOnly();

        private readonly List<ProjectFolder> _folders = new();
        public IReadOnlyCollection<ProjectFolder> Folders => _folders.AsReadOnly();

        private readonly List<ProjectList> _lists = new();
        public IReadOnlyCollection<ProjectList> Lists => _lists.AsReadOnly();

        private ProjectSpace() { } // For EF Core

        internal ProjectSpace(Guid id, Guid workspaceId, string name, string? description,
            string icon, string color, Visibility visibility, int orderIndex, Guid creatorId)
        {
            Id = id;
            ProjectWorkspaceId = workspaceId;
            Name = name;
            Description = description;
            Icon = icon;
            Color = color;
            Visibility = visibility;
            OrderIndex = orderIndex;
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
            AddDomainEvent(new SpaceBasicInfoUpdatedEvent(Id, name, description));
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
            AddDomainEvent(new SpaceVisualSettingsUpdatedEvent(Id, color, icon));
        }

        public void ChangeVisibility(Visibility newVisibility)
        {
            if (Visibility == newVisibility) return;

            Visibility = newVisibility;
            UpdateTimestamp();
            AddDomainEvent(new SpaceVisibilityChangedEvent(Id, newVisibility));

            // CASCADE: only tighten visibility (Public -> Private)
            CascadeVisibilityToChildren(newVisibility);
        }

        public void Archive()
        {
            if (IsArchived) return;

            IsArchived = true;
            UpdateTimestamp();
            AddDomainEvent(new SpaceArchivedEvent(Id));

            ArchiveAllChildren();
        }

        public void Unarchive()
        {
            if (!IsArchived) return;

            IsArchived = false;
            UpdateTimestamp();
            AddDomainEvent(new SpaceUnarchivedEvent(Id));

            UnarchiveAllChildren();
        }

        public void UpdateOrderIndex(int newIndex) => OrderIndex = newIndex;

        // === MEMBERSHIP MANAGEMENT ===

        public void AddMember(Guid userId)
        {
            ValidateGuid(userId, nameof(userId));

            if (_members.Any(m => m.UserId == userId))
                throw new InvalidOperationException("User is already a member of this space.");

            var member = UserProjectSpace.Create(userId, Id);
            _members.Add(member);
            UpdateTimestamp();
            AddDomainEvent(new MemberAddedToSpaceEvent(Id, userId));

            // Cascade membership to private child folders (pre-check to avoid exceptions)
            foreach (var f in _folders.Where(f => f.Visibility == Visibility.Private))
            {
                if (!f.Members.Any(m => m.UserId == userId))
                    f.AddMember(userId);
            }
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
            AddDomainEvent(new MemberRemovedFromSpaceEvent(Id, userId));

            foreach (var f in _folders.Where(f => f.Visibility == Visibility.Private))
            {
                var exists = f.Members.Any(m => m.UserId == userId);
                if (exists)
                    f.RemoveMember(userId);
            }
        }

        // === CHILD ENTITY MANAGEMENT (Folders & Lists) ===

        public ProjectFolder CreateFolder(string name, string? description, Guid creatorId)
        {
            if (IsArchived)
                throw new InvalidOperationException("Cannot create folders in an archived space.");

            name = name?.Trim() ?? string.Empty;
            description = string.IsNullOrWhiteSpace(description?.Trim()) ? null : description.Trim();

            ValidateFolderCreation(name);

            if (_folders.Any(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"A folder with the name '{name}' already exists.");

            var orderIndex = _folders.Count;
            var folder = new ProjectFolder(Guid.NewGuid(), ProjectWorkspaceId, Id, name, description, Visibility, orderIndex, creatorId);
            _folders.Add(folder);

            UpdateTimestamp();
            AddDomainEvent(new FolderCreatedInSpaceEvent(Id, folder.Id, name, creatorId));
            return folder;
        }

        /// <summary>
        /// RemoveFolder requires an IFolderUsageChecker to ensure it's safe to delete.
        /// </summary>
        public void RemoveFolder(Guid folderId)
        {
            throw new InvalidOperationException("This operation requires a domain-service check. Use RemoveFolder(Guid folderId, IFolderUsageChecker usageChecker).");
        }

        public async Task RemoveFolder(Guid folderId, IFolderUsageChecker usageChecker)
        {
            if (usageChecker == null) throw new ArgumentNullException(nameof(usageChecker));

            var folder = _folders.FirstOrDefault(f => f.Id == folderId);
            if (folder == null)
                throw new InvalidOperationException("Folder not found in this space.");

            if ( await usageChecker.IsInUseAsync(folderId))
                throw new InvalidOperationException("Cannot remove folder because it contains lists/tasks according to the provided checker.");

            _folders.Remove(folder);
            UpdateTimestamp();
            AddDomainEvent(new FolderRemovedFromSpaceEvent(Id, folderId, folder.Name));
        }

        public ProjectList CreateList(string name, string? description, string color, Guid creatorId, Guid? folderId = null)
        {
            if (IsArchived)
                throw new InvalidOperationException("Cannot create lists in an archived space.");

            name = name?.Trim() ?? string.Empty;
            description = string.IsNullOrWhiteSpace(description?.Trim()) ? null : description.Trim();
            color = color?.Trim() ?? string.Empty;

            ValidateListCreation(name);

            if (_lists.Any(l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && l.ProjectFolderId == folderId))
                throw new InvalidOperationException($"A list with the name '{name}' already exists in this container.");

            var orderIndex = _lists.Count;
            var list = new ProjectList(Guid.NewGuid(), ProjectWorkspaceId, Id, folderId, name, description, Visibility, orderIndex, creatorId);
            _lists.Add(list);

            UpdateTimestamp();
            AddDomainEvent(new ListCreatedInSpaceEvent(Id, list.Id, name, null, creatorId));
            return list;
        }

        /// <summary>
        /// RemoveList requires an IListUsageChecker to ensure it's safe to delete.
        /// </summary>
        public void RemoveList(Guid listId)
        {
            throw new InvalidOperationException("This operation requires a domain-service check. Use RemoveList(Guid listId, IListUsageChecker usageChecker).");
        }

        public async Task RemoveList(Guid listId, IListUsageChecker usageChecker)
        {
            if (usageChecker == null) throw new ArgumentNullException(nameof(usageChecker));

            var list = _lists.FirstOrDefault(l => l.Id == listId);
            if (list == null)
                throw new InvalidOperationException("List not found in this space.");

            if (await usageChecker.IsInUseAsync(listId))
                throw new InvalidOperationException("Cannot remove list because it contains tasks according to the provided checker.");

            _lists.Remove(list);
            UpdateTimestamp();
            AddDomainEvent(new ListRemovedFromSpaceEvent(Id, listId, list.Name));
        }

        public void MoveListToFolder(Guid listId, Guid? newFolderId)
        {
            var list = _lists.FirstOrDefault(l => l.Id == listId);
            if (list == null)
                throw new InvalidOperationException("List not found in this space.");

            Guid? oldFolderId = list.ProjectFolderId;

            if (newFolderId.HasValue)
            {
                var folder = _folders.FirstOrDefault(f => f.Id == newFolderId.Value);
                if (folder == null)
                    throw new InvalidOperationException("Target folder not found in this space.");
            }

            if (oldFolderId == newFolderId) return;

            list.MoveToFolder(newFolderId);

            UpdateTimestamp();
            AddDomainEvent(new ListMovedToFolderEvent(Id, listId, oldFolderId, newFolderId));
        }

        internal ProjectList DetachList(Guid listId)
        {
            var list = _lists.FirstOrDefault(l => l.Id == listId);
            if (list == null) throw new InvalidOperationException("List not found in this space.");

            _lists.Remove(list);
            UpdateTimestamp();
            return list;
        }

        internal void AttachList(ProjectList list, Guid? folderId = null)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));

            if (_lists.Any(l => l.Name.Equals(list.Name, StringComparison.OrdinalIgnoreCase) && l.ProjectFolderId == folderId))
                throw new InvalidOperationException($"A list with the name '{list.Name}' already exists in this container.");

            list.MoveToSpace(Id);
            _lists.Add(list);

            UpdateTimestamp();
            AddDomainEvent(new ListAttachedToSpaceEvent(Id, list.Id, folderId));
        }

        public void ReorderFolders(List<Guid> folderIds)
        {
            if (folderIds.Count != _folders.Count)
                throw new ArgumentException("Must provide all folder IDs for reordering.", nameof(folderIds));

            var allExist = folderIds.All(id => _folders.Any(f => f.Id == id));
            if (!allExist)
                throw new ArgumentException("One or more folder IDs are invalid.", nameof(folderIds));

            for (int i = 0; i < folderIds.Count; i++)
            {
                var folder = _folders.First(f => f.Id == folderIds[i]);
                folder.UpdateOrderIndex(i);
            }

            UpdateTimestamp();
            AddDomainEvent(new FoldersReorderedInSpaceEvent(Id, folderIds));
        }

        public void ReorderLists(List<Guid> listIds)
        {
            if (listIds.Count != _lists.Count)
                throw new ArgumentException("Must provide all list IDs for reordering.", nameof(listIds));

            var allExist = listIds.All(id => _lists.Any(l => l.Id == id));
            if (!allExist)
                throw new ArgumentException("One or more list IDs are invalid.", nameof(listIds));

            for (int i = 0; i < listIds.Count; i++)
            {
                var list = _lists.First(l => l.Id == listIds[i]);
                list.UpdateOrderIndex(i);
            }

            UpdateTimestamp();
            AddDomainEvent(new ListsReorderedInSpaceEvent(Id, listIds));
        }

        // === BULK OPERATIONS (BATCH_RULE with safety) ===

        private void ArchiveAllChildren()
        {
            var foldersToArchive = _folders.Where(f => !f.IsArchived).ToList();
            var listsToArchive = _lists.Where(l => !l.IsArchived).ToList();

            var total = foldersToArchive.Count + listsToArchive.Count;
            if (total > MAX_BATCH_SIZE)
                throw new InvalidOperationException($"Batch too large. Reduce size below {MAX_BATCH_SIZE} or run as smaller batches.");

            foreach (var f in foldersToArchive) f.Archive();
            foreach (var l in listsToArchive) l.Archive();

            UpdateTimestamp();
            AddDomainEvent(new AllChildrenArchivedInSpaceEvent(Id));
        }

        private void UnarchiveAllChildren()
        {
            var foldersToUnarchive = _folders.Where(f => f.IsArchived).ToList();
            var listsToUnarchive = _lists.Where(l => l.IsArchived).ToList();

            var total = foldersToUnarchive.Count + listsToUnarchive.Count;
            if (total > MAX_BATCH_SIZE)
                throw new InvalidOperationException($"Batch too large. Reduce size below {MAX_BATCH_SIZE} or run as smaller batches.");

            foreach (var f in foldersToUnarchive) f.Unarchive();
            foreach (var l in listsToUnarchive) l.Unarchive();

            UpdateTimestamp();
            AddDomainEvent(new AllChildrenUnarchivedInSpaceEvent(Id));
        }

        // === PRIVATE CASCADE METHODS ===

        private void CascadeVisibilityToChildren(Visibility newVisibility)
        {
            if (newVisibility == Visibility.Private)
            {
                foreach (var f in _folders.Where(f => f.Visibility == Visibility.Public))
                    f.ChangeVisibility(Visibility.Private);
                foreach (var l in _lists.Where(l => l.Visibility == Visibility.Public))
                    l.ChangeVisibility(Visibility.Private);
            }
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

        private static void ValidateFolderCreation(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Folder name cannot be empty.", nameof(name));
            if (name.Length > 100)
                throw new ArgumentException("Folder name cannot exceed 100 characters.", nameof(name));
        }

        private static void ValidateListCreation(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("List name cannot be empty.", nameof(name));
            if (name.Length > 100)
                throw new ArgumentException("List name cannot exceed 100 characters.", nameof(name));
        }

        private static void ValidateGuid(Guid id, string paramName)
        {
            if (id == Guid.Empty) throw new ArgumentException("Guid cannot be empty.", paramName);
        }
    }
}
