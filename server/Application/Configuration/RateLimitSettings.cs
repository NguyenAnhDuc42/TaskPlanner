namespace Application;

public class RateLimitSettings
{
    public const string SectionName = "RateLimitSettings";
    
    public int MaxRequestsPerMinute { get; set; } = 200;
}

