using System.Linq.Expressions;

namespace Application.Interfaces;

/// <summary>
/// Service for enqueuing background jobs.
/// Jobs run asynchronously, outside the request lifecycle.
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// Enqueue a job to run immediately (but async, not blocking request).
    /// </summary>
    void Enqueue<T>(Expression<Func<T, Task>> methodCall);

    /// <summary>
    /// Schedule a job to run after a delay.
    /// </summary>
    void Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay);
}
