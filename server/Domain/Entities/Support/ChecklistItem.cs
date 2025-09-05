using System;
using Domain.Common;

namespace Domain.Entities.Support;

public class ChecklistItem : Entity
{
    public Guid ChecklistId { get; private set; }
    public string Text { get; private set; } = null!;
    public bool IsCompleted { get; private set; }
    public long OrderKey { get; private set; }

    private ChecklistItem() { } // EF Core

    private ChecklistItem(Guid id, string text, long orderKey, Guid checklistId)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Checklist item text cannot be empty.", nameof(text));
        if (checklistId == Guid.Empty) throw new ArgumentException("TaskChecklistId cannot be empty.", nameof(checklistId));
        if (orderKey < 0) throw new ArgumentOutOfRangeException(nameof(orderKey));

        Text = text.Trim();
        OrderKey = orderKey;
        ChecklistId = checklistId;
        IsCompleted = false;
    }

    public static ChecklistItem Create(string text, long orderKey, Guid taskChecklistId)
        => new ChecklistItem(Guid.NewGuid(), text, orderKey, taskChecklistId);

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

    public void UpdateOrderKey(long newKey)
    {
        if (newKey < 0) throw new ArgumentOutOfRangeException(nameof(newKey));
        if (OrderKey == newKey) return;

        OrderKey = newKey;
        UpdateTimestamp();
    }
}
