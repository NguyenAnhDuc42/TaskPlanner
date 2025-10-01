using System;

namespace Infrastructure.Events.IntergrationsEvents;
/// <summary>
/// Utility for computing exponential backoff with jitter for retry delays.
/// </summary>
public static class BackoffCalculator
{
    /// <summary>
    /// Computes backoff duration with exponential increase and jitter.
    /// 
    /// TAKES: 
    ///   - initialDelaySeconds (int from config)
    ///   - multiplier (double from config, e.g., 2.0 for doubling)
    ///   - attempt (int, 1-based: 1st retry, 2nd retry, etc.)
    ///   - maxBackoffSeconds (double, default 300 = 5 minutes cap)
    ///   - jitterFactor (double, default 0.2 = ±20% randomness)
    /// DOES:
    ///   1. Calculate base delay: initialDelaySeconds * (multiplier ^ (attempt - 1))
    ///   2. Cap at maxBackoffSeconds
    ///   3. Apply jitter: baseDelay * (1 + Random(-jitterFactor, +jitterFactor))
    ///   4. Example: initialDelay=5s, multiplier=2, attempts:
    ///      - Attempt 1: ~5s (±1s jitter)
    ///      - Attempt 2: ~10s (±2s jitter)
    ///      - Attempt 3: ~20s (±4s jitter)
    ///      - Attempt 4: ~40s (±8s jitter)
    ///      - Attempt 5: ~80s (±16s jitter)
    /// RETURNS: TimeSpan (delay duration)
    /// CONDITION: Called by OutboxWorker and KafkaConsumerWorker when scheduling retries
    /// LOGIC: Jitter prevents thundering herd when multiple messages fail simultaneously
    /// </summary>
    public static TimeSpan ComputeBackoff(int initialDelaySeconds, double multiplier, int attempt, double maxBackoffSeconds = 300.0, double jitterFactor = 0.2)
    {
        if (attempt < 1) attempt = 1;
        if (initialDelaySeconds < 0) initialDelaySeconds = 0;
        if (multiplier < 1.0) multiplier = 1.0;

        // Base exponential backoff
        double baseDelay = initialDelaySeconds * Math.Pow(multiplier, attempt - 1);

        // Cap at max
        baseDelay = Math.Min(baseDelay, maxBackoffSeconds);

        // Apply jitter: ±jitterFactor randomness
        var random = new Random();
        double jitter = 1.0 + ((random.NextDouble() * 2.0 - 1.0) * jitterFactor);
        double finalDelay = baseDelay * jitter;

        return TimeSpan.FromSeconds(Math.Max(0, finalDelay));
    }
}
