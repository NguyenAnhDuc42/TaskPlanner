using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace Api;

public class IdempotencyMiddleware(RequestDelegate next, IMemoryCache cache)
{
    private static readonly ConcurrentDictionary<string, bool> _inFlightRequests = new();

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Method == HttpMethods.Get || 
            context.Request.Method == HttpMethods.Options || 
            context.Request.Method == HttpMethods.Head)
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-Idempotency-Key", out var keyValues))
        {
            await next(context);
            return;
        }

        var idempotencyKey = keyValues.ToString();
        
        var workspaceId = context.Items["WorkspaceId"]?.ToString() ?? "global";
        var userId = context.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? "anonymous";
        var cacheKey = $"Idempotency_{workspaceId}_{userId}_{idempotencyKey}";

        if (cache.TryGetValue(cacheKey, out CachedResponse? cachedResponse) && cachedResponse != null)
        {
            context.Response.StatusCode = cachedResponse.StatusCode;
            
            if (cachedResponse.StatusCode != 204 && cachedResponse.StatusCode != 205 && cachedResponse.StatusCode != 304)
            {
                context.Response.ContentType = "application/json";
                if (!string.IsNullOrEmpty(cachedResponse.Body))
                {
                    await context.Response.WriteAsync(cachedResponse.Body);
                }
            }
            return;
        }

        if (!_inFlightRequests.TryAdd(cacheKey, true))
        {
            context.Response.StatusCode = 425; 
            await context.Response.WriteAsync("Duplicate request is already in progress.");
            return;
        }

        try
        {
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await next(context);

            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
                context.Response.Body.Seek(0, SeekOrigin.Begin);

                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3),
                    Size = Math.Max(responseText.Length, 1)
                };

                cache.Set(cacheKey, new CachedResponse(context.Response.StatusCode, responseText), cacheEntryOptions);
            }

            if (context.Response.StatusCode != 204 && context.Response.StatusCode != 205 && context.Response.StatusCode != 304)
            {
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
        finally
        {
            _inFlightRequests.TryRemove(cacheKey, out _);
        }
    }
}

public record CachedResponse(int StatusCode, string Body);

