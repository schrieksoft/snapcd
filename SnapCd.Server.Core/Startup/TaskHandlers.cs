
using SnapCd.Server.Core.Hubs.Handlers;

namespace SnapCd.Server.Core.Startup;

public static class TaskHandlers
{
    public static IServiceCollection AddSnapCdTaskHandlers(this IServiceCollection services)
    {
        services.AddScoped<GetDefinitiveRevisionHandler>();
        services.AddScoped<GetModuleHandler>();
        services.AddScoped<InitHandler>();
        services.AddScoped<ValidateHandler>();
        services.AddScoped<VariableHandler>();
        services.AddScoped<PlanHandler>();
        services.AddScoped<PlanDestroyHandler>();
        services.AddScoped<ApplyFromPlanHandler>();
        services.AddScoped<DestroyFromPlanHandler>();
        services.AddScoped<OutputHandler>();
        services.AddScoped<SourceRefreshHandler>();
        services.AddScoped<ReportRunningTaskHandler>();
        services.AddScoped<CancelKillHandler>();
        services.AddScoped<CancelGracefulHandler>();

        return services;
    }
}

