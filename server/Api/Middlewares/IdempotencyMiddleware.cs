using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace Api.Middlewares;

public class IdempotencyMiddleware(RequestDelegate next, IMemoryCache cache)
{
    // Phase 1: In-Memory atomic lock to prevent duplicate concurrent requests
    private static readonly ConcurrentDictionary<string, bool> _inFlightRequests = new();

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply idempotency to mutating endpoints
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
        
        // Tie the key to the specific user/workspace to prevent collisions across tenants
        var userId = context.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? "anonymous";
        var cacheKey = $"Idempotency_{userId}_{idempotencyKey}";

        // Phase 2 Check: Has this request already been completed recently?
        if (cache.TryGetValue(cacheKey, out CachedResponse? cachedResponse) && cachedResponse != null)
        {
            context.Response.StatusCode = cachedResponse.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(cachedResponse.Body);
            return;
        }

        // Phase 1 Check: Is this exact request currently executing?
        if (!_inFlightRequests.TryAdd(cacheKey, true))
        {
            // 425 Too Early tells the client "I'm still working on this exact request, hold on"
            context.Response.StatusCode = 425; 
            await context.Response.WriteAsync("Duplicate request is already in progress.");
            return;
        }

        try
        {
            // We need to capture the response body to cache it
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Execute the actual handler / controller
            await next(context);

            // If it was a successful mutation, we cache it for 3 minutes
            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
                context.Response.Body.Seek(0, SeekOrigin.Begin);

                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3),
                    Size = responseText.Length // We MUST define size because we hard-capped our MemoryCache to 50MB
                };

                cache.Set(cacheKey, new CachedResponse(context.Response.StatusCode, responseText), cacheEntryOptions);
            }

            // Copy the intercepted response back to the real outgoing stream
            await responseBody.CopyToAsync(originalBodyStream);
        }
        finally
        {
            // Always release the atomic lock when finished, regardless of success/fail
            _inFlightRequests.TryRemove(cacheKey, out _);
        }
    }
}

public record CachedResponse(int StatusCode, string Body);
