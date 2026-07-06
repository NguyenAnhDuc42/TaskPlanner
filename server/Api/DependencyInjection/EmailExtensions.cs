namespace Api;

public static class EmailExtensions
{
    public static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<EmailSettings>(config.GetSection(EmailSettings.SectionName));
        services.AddTransient<SmtpEmailService>();

        return services;
    }
}
