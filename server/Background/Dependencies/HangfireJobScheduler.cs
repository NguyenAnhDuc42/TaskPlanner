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
    }
}
