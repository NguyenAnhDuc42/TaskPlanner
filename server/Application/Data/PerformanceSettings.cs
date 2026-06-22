namespace Application;

public class PerformanceSettings
{
    public const string SectionName = "PerformanceSettings";

    public int DbContextPoolSize { get; set; } = 128;
    public int DatabaseMaxRetryCount { get; set; } = 3;
    public int DatabaseMaxRetryDelaySeconds { get; set; } = 2;
}

