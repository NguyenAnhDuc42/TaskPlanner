using Domain.Common;
using System;
using System.Collections.Generic;

namespace Domain.Entities.Support;

public class Checklist : Entity
{
    public string Title { get; private set; } = null!;
    public Guid ProjectTaskId { get; private set; }
    private readonly List<ChecklistItem> _items = new();
    public IReadOnlyCollection<ChecklistItem> Items => _items.AsReadOnly();

    private Checklist() { } // For EF Core

    private Checklist(Guid id, string title, Guid projectTaskId)
    {
        Id = id;
        Title = title;
        ProjectTaskId = projectTaskId;
    }

    public static Checklist Create(string title, Guid projectTaskId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Checklist title cannot be empty.", nameof(title));

        return new Checklist(Guid.NewGuid(), title, projectTaskId);
    }

    public ChecklistItem AddItem(string text, Guid? assigneeId = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Checklist item text cannot be empty.", nameof(text));

        var item = new ChecklistItem(text, assigneeId, _items.Count);
        _items.Add(item);
        return item;
    }
}