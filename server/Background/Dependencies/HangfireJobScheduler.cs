using Background.Services;
using Hangfire;

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
        _recurringJobManager.AddOrUpdate<DatabaseKeepAliveService>(
            "database-keep-alive",
            service => service.KeepAlive(),
            "*/5 * * * *" 
        );
    }
}
