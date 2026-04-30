using Hangfire;
using Quartz;

namespace SnapCd.Server.Core.Startup;

public static class BackgroundJobCofigurationExtensions
{
    public static IServiceCollection AddSnapCdBackgroundJobs(this IServiceCollection services, string connectionString)
    {
        // Register the Quartz.NET service and configure it to block shutdown until jobs are complete.
        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);


        services.AddQuartz(q =>
        {
            q.UseSimpleTypeLoader();
            q.UseInMemoryStore();
        });


        services.AddHangfire(config =>
        {
            config.UseSqlServerStorage(connectionString, new Hangfire.SqlServer.SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true,
                SchemaName = "HangFire"
            });
        });


        services.AddHangfireServer(options =>
        {
            options.WorkerCount = Environment.ProcessorCount * 5;
            options.Queues = new[] { "default" };
            options.ServerTimeout = TimeSpan.FromMinutes(4);
            options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
            options.HeartbeatInterval = TimeSpan.FromSeconds(30);
            options.ServerCheckInterval = TimeSpan.FromMinutes(1);
            options.CancellationCheckInterval = TimeSpan.FromSeconds(5);
        });


        return services;
    }
}