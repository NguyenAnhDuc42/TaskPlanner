using System.Linq.Expressions;
using Application.Interfaces;
using Hangfire;

namespace Background.Services;

public class HangfireBackgroundJobService : IBackgroundJobService
{
    public void Enqueue<T>(Expression<Func<T, Task>> methodCall)
    {
        BackgroundJob.Enqueue(methodCall);
    }

    public void Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay)
    {
        BackgroundJob.Schedule(methodCall, delay);
    }

    public void TriggerOutbox()
    {
        BackgroundJob.Enqueue<Jobs.ProcessOutboxJob>(x => x.RunAsync());
    }
}
