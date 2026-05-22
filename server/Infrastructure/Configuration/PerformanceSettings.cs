namespace Infrastructure.Configuration;

public class PerformanceSettings
{
    public int DbContextPoolSize { get; set; } = 128;
    public int DatabaseMaxRetryCount { get; set; } = 3;
    public int DatabaseMaxRetryDelaySeconds { get; set; } = 2;
    public long MemoryCacheLimitMB { get; set; } = 50;
    public int RateLimitMaxRequestsPerMinute { get; set; } = 200;
}
