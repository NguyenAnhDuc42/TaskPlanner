using Hangfire;
using Background.Jobs;

namespace Background.Dependencies;

public static class HangfireJobScheduler
{
    public static void ScheduleJobs()
    {
        // Process outbox messages every minute
        RecurringJob.AddOrUpdate<ProcessOutboxJob>(
            "process-outbox",
            job => job.RunAsync(),
            Cron.Minutely());

        // You can add other recurring jobs here
    }
}
