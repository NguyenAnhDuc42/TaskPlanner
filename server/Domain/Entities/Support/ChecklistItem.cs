using System;
using Domain.Common;

namespace Domain.Entities.Support;

public class ChecklistItem : Entity
{
    public Guid TaskChecklistId { get; private set; }
    public string Text { get; private set; } = null!;
    public bool IsCompleted { get; private set; }
    public int OrderIndex { get; private set; }

    private ChecklistItem() { } // EF Core

    private ChecklistItem(Guid id, string text, int orderIndex, Guid taskChecklistId)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Checklist item text cannot be empty.", nameof(text));
        if (taskChecklistId == Guid.Empty) throw new ArgumentException("TaskChecklistId cannot be empty.", nameof(taskChecklistId));
        if (orderIndex < 0) throw new ArgumentOutOfRangeException(nameof(orderIndex));

        Text = text.Trim();
        OrderIndex = orderIndex;
        TaskChecklistId = taskChecklistId;
        IsCompleted = false;
    }

    public static ChecklistItem Create(string text, int orderIndex, Guid taskChecklistId)
        => new ChecklistItem(Guid.NewGuid(), text, orderIndex, taskChecklistId);

    public void Toggle()
    {
        IsCompleted = !IsCompleted;
        UpdateTimestamp();
    }

    public void UpdateText(string newText)
    {
        if (string.IsNullOrWhiteSpace(newText)) throw new ArgumentException("Checklist item text cannot be empty.", nameof(newText));
        if (Text == newText.Trim()) return;

        Text = newText.Trim();
        UpdateTimestamp();
    }

    public void UpdateOrder(int newOrder)
    {
        if (newOrder < 0) throw new ArgumentOutOfRangeException(nameof(newOrder));
        if (OrderIndex == newOrder) return;

        OrderIndex = newOrder;
        UpdateTimestamp();
    }
}
