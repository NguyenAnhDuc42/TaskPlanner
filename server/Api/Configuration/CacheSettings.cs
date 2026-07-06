namespace Api;

public class CacheSettings
{
    public const string SectionName = "CacheSettings";
    
    public long MemoryCacheLimitMB { get; set; } = 50;
}

