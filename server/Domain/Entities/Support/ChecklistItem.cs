using System;
using Domain.Common;

namespace Domain.Entities.Support;

public class ChecklistItem : Entity
{
    public Guid TaskChecklistId { get; private set; }
    public string Text { get; private set; } = null!;
    public bool IsCompleted { get; private set; }
    public int OrderIndex { get; private set; }

    // Private constructor for EF Core
    private ChecklistItem() { }

    private ChecklistItem(Guid id, string text, int orderIndex)
    {
        Id = id;
        Text = text;
        IsCompleted = false; // Default to not completed
        OrderIndex = orderIndex;
        TaskChecklistId = Guid.Empty; // Initialize TaskChecklistId
    }

    public static ChecklistItem Create(string text, int orderIndex)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Checklist item text cannot be empty.", nameof(text));

        return new ChecklistItem(Guid.NewGuid(), text, orderIndex);
    }

    public void Toggle()
    {
        IsCompleted = !IsCompleted;
    }

    public void UpdateText(string newText)
    {
        if (string.IsNullOrWhiteSpace(newText))
            throw new ArgumentException("Checklist item text cannot be empty.", nameof(newText));
        Text = newText;
    }

    public void UpdateOrder(int newOrder)
    {
        if (newOrder < 0) 
            throw new ArgumentOutOfRangeException(nameof(newOrder), "Order index cannot be negative.");
        OrderIndex = newOrder;
    }

}