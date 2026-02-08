using Hangfire;
using Background.Jobs;

namespace Background.Dependencies;

public  class HangfireJobScheduler
{
    private readonly IRecurringJobManager _recurringJobManager;

    public HangfireJobScheduler(IRecurringJobManager recurringJobManager)
    {
        _recurringJobManager = recurringJobManager;
    }

    public void Schedule()
    {
        _recurringJobManager.AddOrUpdate<ProcessOutboxJob>(
            "process-outbox",
            job => job.RunAsync(),
            Cron.Minutely);
    }
}
