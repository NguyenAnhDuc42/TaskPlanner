namespace Application;

public class OAuthSettings
{
    public const string SectionName = "OAuth";

    public ProviderSettings Google { get; set; } = new();
    public ProviderSettings GitHub { get; set; } = new();

    public class ProviderSettings
    {
        public string ClientId { get; set; } = "";
        public string ClientSecret { get; set; } = "";
    }
}
