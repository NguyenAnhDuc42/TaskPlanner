using System;

namespace Domain.Entities.Support;

public class ChecklistItem
{
    public Guid Id { get; private set; }
    public string Text { get; private set; } = null!;
    public bool IsCompleted { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public int OrderIndex { get; private set; }

    // Private constructor for EF Core
    private ChecklistItem() { }

    public ChecklistItem(string text, Guid? assigneeId, int orderIndex)
    {
        Id = Guid.NewGuid();
        Text = text;
        IsCompleted = false; // Default to not completed
        AssigneeId = assigneeId;
        OrderIndex = orderIndex;
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

    public void Assign(Guid? assigneeId)
    {
        AssigneeId = assigneeId;
    }
}