namespace Api;

public static class RedisConnectionHelper
{
    // StackExchange.Redis 2.13.1's ConfigurationOptions.Parse() mishandles a "redis://user:pass@host:port"
    // URI (production/Railway's format) — resolves to an endpoint with the port duplicated
    // (host:port:port), which can never connect. Confirmed via an isolated diagnostic endpoint with
    // no SignalR involved, so it's the string parser itself, not networking/credentials/SignalR.
    //
    // Local dev (Aspire's AddRedis()) hands the app a completely different, scheme-less format —
    // StackExchange.Redis's own native "host:port" syntax — which .Parse() handles correctly; only
    // the redis:// URI scheme trips it up. Passing a scheme-less string like "localhost:6380" to
    // System.Uri is actively wrong: Uri parses it as scheme="localhost", leaving Host empty (the
    // exact crash this caused before this branch was added). So: URI-parse only when a scheme is
    // actually present, otherwise defer to .Parse() for its native format.
    public static StackExchange.Redis.ConfigurationOptions ParseRedisUrl(string redisUrl)
    {
        if (!redisUrl.Contains("://"))
            return StackExchange.Redis.ConfigurationOptions.Parse(redisUrl);

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
