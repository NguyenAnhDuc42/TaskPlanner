namespace Api;

public static class RedisConnectionHelper
{
    // StackExchange.Redis 2.13.1's ConfigurationOptions.Parse() mishandles this project's
    // "redis://user:password@host:port" connection string — it resolves to an endpoint with the
    // port duplicated (host:port:port), which can never connect. Confirmed via an isolated
    // diagnostic endpoint with no SignalR involved, so it's the string parser itself, not
    // networking, not credentials, not the SignalR backplane. Parsing the URI ourselves with
    // System.Uri and building ConfigurationOptions directly from its pieces sidesteps whatever bug
    // exists in that parser entirely.
    public static StackExchange.Redis.ConfigurationOptions ParseRedisUrl(string redisUrl)
    {
        var uri = new Uri(redisUrl);
        var options = new StackExchange.Redis.ConfigurationOptions
        {
            EndPoints = { { uri.Host, uri.Port } },
        };

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var parts = uri.UserInfo.Split(':', 2);
            if (parts.Length > 0 && !string.IsNullOrEmpty(parts[0])) options.User = parts[0];
            if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1])) options.Password = parts[1];
        }

        return options;
    }
}
