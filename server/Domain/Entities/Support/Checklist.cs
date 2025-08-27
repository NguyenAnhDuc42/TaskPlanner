using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Common;

namespace Domain.Entities.Support;

public class Checklist : Entity
{
    public Guid ProjectTaskId { get; private set; }
    public string Title { get; private set; } = null!;
    public int OrderIndex { get; private set; }

    private readonly List<ChecklistItem> _items = new();
    public IReadOnlyCollection<ChecklistItem> Items => _items.AsReadOnly();

    private Checklist() { } // EF Core

    private Checklist(Guid id, string title, Guid projectTaskId)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Checklist title cannot be empty.", nameof(title));
        if (projectTaskId == Guid.Empty) throw new ArgumentException("ProjectTaskId cannot be empty.", nameof(projectTaskId));

        Title = title.Trim();
        ProjectTaskId = projectTaskId;
        OrderIndex = 0;
    }

    public static Checklist Create(string title, Guid projectTaskId)
        => new Checklist(Guid.NewGuid(), title, projectTaskId);

    public ChecklistItem AddItem(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Checklist item text cannot be empty.", nameof(text));

        var item = ChecklistItem.Create(text, _items.Count, Id);
        _items.Add(item);
        UpdateTimestamp();
        return item;
    }

    public void RemoveItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return;

        _items.Remove(item);

        // Re-order remaining items
        for (int i = 0; i < _items.Count; i++)
            _items[i].UpdateOrder(i);

        UpdateTimestamp();
    }

    public void ToggleItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item is null) throw new InvalidOperationException("Item not found in this checklist.");

        item.Toggle();
        UpdateTimestamp();
    }

    public void UpdateItemText(Guid itemId, string newText)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item is null) throw new InvalidOperationException("Item not found in this checklist.");

        item.UpdateText(newText);
        UpdateTimestamp();
    }

    public void ReorderItem(Guid itemId, int newPosition)
    {
        var itemToMove = _items.FirstOrDefault(i => i.Id == itemId);
        if (itemToMove is null) throw new InvalidOperationException("Item not found in this checklist.");
        if (newPosition < 0 || newPosition >= _items.Count) throw new ArgumentOutOfRangeException(nameof(newPosition));

        _items.Remove(itemToMove);
        _items.Insert(newPosition, itemToMove);

        for (int i = 0; i < _items.Count; i++)
            _items[i].UpdateOrder(i);

        UpdateTimestamp();
    }

    public void UpdateTitle(string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle)) throw new ArgumentException("Checklist title cannot be empty.", nameof(newTitle));
        if (Title == newTitle.Trim()) return;

        Title = newTitle.Trim();
        UpdateTimestamp();
    }
}
