namespace Application;

public class AppSettings
{
    public const string SectionName = "AppSettings";

    public string FrontendUrl { get; set; } = "https://localhost:5173";
    public string BackendUrl { get; set; } = "https://localhost:7285";
}
