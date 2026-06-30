namespace Domain;

public class ProcessedTrace
{
    public string TraceId { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
