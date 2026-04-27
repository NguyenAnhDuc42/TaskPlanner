using Domain.Exceptions;

namespace Domain.Entities;

public readonly record struct TaskEstimation
{
    public int? StoryPoints { get; init; }
    public long? TimeEstimateSeconds { get; init; }

    public TaskEstimation(int? storyPoints, long? timeEstimateSeconds)
    {
        if (storyPoints.HasValue && storyPoints < 0)
            throw new BusinessRuleException("Story points cannot be negative.");

        if (timeEstimateSeconds.HasValue && timeEstimateSeconds < 0)
            throw new BusinessRuleException("Time estimate cannot be negative.");

        StoryPoints = storyPoints;
        TimeEstimateSeconds = timeEstimateSeconds;
    }

    public static TaskEstimation Create(int? storyPoints, long? timeEstimateSeconds)
        => new(storyPoints, timeEstimateSeconds);

    public static TaskEstimation Empty => new(null, null);
    
    public double? Hours => TimeEstimateSeconds.HasValue ? TimeEstimateSeconds.Value / 3600.0 : null;
}
