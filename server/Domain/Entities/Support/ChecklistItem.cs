using System;

namespace Domain.Entities.Support;

public class ChecklistItem
{
    public string Text { get; private set; } = null!;
    public bool IsCompleted { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public int OrderIndex { get; private set; }

    // Private constructor for EF Core
    private ChecklistItem() { }

    public ChecklistItem(string text, Guid? assigneeId, int orderIndex)
    {
        Text = text;
        IsCompleted = false; // Default to not completed
        AssigneeId = assigneeId;
        OrderIndex = orderIndex;
    }

    public void MarkCompleted()
    {
        IsCompleted = true;
    }

    public void MarkIncomplete()
    {
        IsCompleted = false;
    }

    public void UpdateText(string newText)
    {
        if (string.IsNullOrWhiteSpace(newText))
            throw new ArgumentException("Checklist item text cannot be empty.", nameof(newText));
        Text = newText;
    }

    public void Assign(Guid? assigneeId)
    {
        AssigneeId = assigneeId;
    }
}