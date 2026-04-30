using SnapCd.Contracts.Dto.OutputSets;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Runner.Services.Plan;

namespace SnapCd.Runner.Services;

public interface IEngine
{
    string GetInitDir();
    string GetSnapCdDir();

    Task<string> Init(
        Dictionary<string, string> resolvedEnvVars,
        string? beforeHook,
        string? afterHook,
        EngineBackendConfiguration backendConfig,
        EngineFlags flags,
        CancellationToken killCancellationToken = default,
        CancellationToken gracefulCancellationToken = default);

    Task Validate(
        string? beforeHook = null,
        string? afterHook = null,
        CancellationToken killCancellationToken = default,
        CancellationToken gracefulCancellationToken = default);

    Task<string> Plan(
        Dictionary<string, string> parameters,
        string? planBeforeHook,
        string? planAfterHook,
        CancellationToken killCancellationToken = default,
        CancellationToken gracefulCancellationToken = default);

    Task<string> PlanDestroy(
        Dictionary<string, string> parameters,
        string? beforeHook,
        string? afterHook,
        CancellationToken killCancellationToken = default,
        CancellationToken gracefulCancellationToken = default);

    Task<string> ApplyFromPlan(
        string? beforeHook,
        string? afterHook,
        CancellationToken killCancellationToken = default,
        CancellationToken gracefulCancellationToken = default);

    Task<string> DestroyFromPlan(
        string? beforeHook,
        string? afterHook,
        CancellationToken killCancellationToken = default,
        CancellationToken gracefulCancellationToken = default);

    Task<string> Output(
        string? beforeHook,
        string? afterHook,
        CancellationToken killCancellationToken = default,
        CancellationToken gracefulCancellationToken = default);

    Task<int> Statistics(
        CancellationToken killCancellationToken = default,
        CancellationToken gracefulCancellationToken = default);

    Task<int> ReadStatisticsFromFile();

    IParsedPlan ParseApplyPlan();
    IParsedPlan ParseDestroyPlan();

    Task<OutputSetCreateDto?> ParseJsonToModuleOutputSet(
        string json, Dictionary<string, bool>? outputSources = null);
}
