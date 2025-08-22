using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;

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

    public void RemoveItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return;

        _items.Remove(item);
        // Re-order remaining items
        for (int i = 0; i < _items.Count; i++)
        {
            _items[i].UpdateOrder(i);
        }
    }

    public void ToggleItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            throw new InvalidOperationException("Item not found in this checklist.");

        item.Toggle();
    }

    public void UpdateItemText(Guid itemId, string newText)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            throw new InvalidOperationException("Item not found in this checklist.");

        item.UpdateText(newText);
    }

    public void ReorderItem(Guid itemId, int newPosition)
    {
        var itemToMove = _items.FirstOrDefault(i => i.Id == itemId);
        if (itemToMove is null)
            throw new InvalidOperationException("Item not found in this checklist.");

        if (newPosition < 0 || newPosition >= _items.Count)
            throw new ArgumentOutOfRangeException(nameof(newPosition), "Invalid position for reordering.");

        _items.Remove(itemToMove);
        _items.Insert(newPosition, itemToMove);

        // Update order index for all items
        for (int i = 0; i < _items.Count; i++)
        {
            _items[i].UpdateOrder(i);
        }
    }
}