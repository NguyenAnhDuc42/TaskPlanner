namespace Api;

public static class ConfigurationValidationExtensions
{
    public static void ValidateRequiredSecrets(this IConfiguration config)
    {
        var errors = new List<string>();

        var jwtKey = config[$"{JwtSettings.SectionName}:SecretKey"];
        if (string.IsNullOrWhiteSpace(jwtKey))
            errors.Add($"  • {JwtSettings.SectionName}:SecretKey — JWT signing key is missing.\n    Run: dotnet user-secrets set \"{JwtSettings.SectionName}:SecretKey\" \"<32+ char random string>\"");
        else if (jwtKey.Length < 32)
            errors.Add($"  • {JwtSettings.SectionName}:SecretKey — too short ({jwtKey.Length} chars, minimum 32).");

        var connStr = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connStr))
            errors.Add($"  • ConnectionStrings:DefaultConnection — database connection string is missing.\n    Run: dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"<postgres connection string>\"");

        var cursorKey = config[$"{CursorEncryptionOptions.SectionName}:Key"];
        if (string.IsNullOrWhiteSpace(cursorKey))
            errors.Add($"  • {CursorEncryptionOptions.SectionName}:Key — cursor encryption key is missing.\n    Run: dotnet user-secrets set \"{CursorEncryptionOptions.SectionName}:Key\" \"<32+ char random string>\"");

        if (errors.Count > 0)
            throw new InvalidOperationException(
                $"\n\nMissing required configuration. Set the following via user-secrets (dev) or environment variables (prod):\n\n{string.Join("\n\n", errors)}\n");
    }
}
