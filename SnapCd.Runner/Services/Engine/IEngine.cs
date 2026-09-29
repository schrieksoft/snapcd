// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.OutputSets;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Runner.Services.Plan;

namespace SnapCd.Runner.Services;

public interface IEngine
{
    string GetInitDir();

    /// <summary>Runs a command in the module directory with the job's environment loaded.</summary>
    Task<string> RunProcess(
        string script,
        CancellationToken killCancellationToken,
        bool logOutput = true);

    /// <summary>
    /// Moves, imports or removes addresses one at a time, reporting each on its own. A batch never
    /// abandons the rest on one failure: which of them worked is what the caller needs.
    /// </summary>
    /// <summary>Moves each address to its target. A dry run prints what it would move and moves nothing.</summary>
    Task<List<(string Address, bool Succeeded)>> Move(
        IReadOnlyCollection<(string Address, string? Target)> instructions,
        bool dryRun = false,
        CancellationToken killCancellationToken = default);

    /// <summary>Imports each address from the id it already has. There is no dry run for this.</summary>
    Task<List<(string Address, bool Succeeded)>> Import(
        IReadOnlyCollection<(string Address, string? Target)> instructions,
        CancellationToken killCancellationToken = default);

    /// <summary>Takes each address out of state, leaving the infrastructure alone.</summary>
    Task<List<(string Address, bool Succeeded)>> Remove(
        IReadOnlyCollection<string> addresses,
        bool dryRun = false,
        CancellationToken killCancellationToken = default);

    /// <summary>
    /// Which of the given addresses are in this Module's state. The state itself is never returned
    /// or logged - only the verdict on the addresses asked about.
    /// </summary>
    Task<(List<string> Present, List<string> Absent)> StateListFiltered(
        IReadOnlyCollection<string> addresses,
        CancellationToken killCancellationToken = default);
    string GetSnapCdDir();

    Task<string> Init(
        Dictionary<string, string> resolvedEnvVars,
        string? beforeHook,
        string? afterHook,
        EngineBackendConfiguration backendConfig,
        CancellationToken killCancellationToken = default);

    Task Validate(
        string? beforeHook = null,
        string? afterHook = null,
        CancellationToken killCancellationToken = default);

    /// <summary>
    /// Exports the current binary plan as JSON for policy evaluation and returns the file path.
    /// The document never leaves the runner.
    /// </summary>
    Task<string> ExportPlanJson(
        bool isDestroyJob,
        CancellationToken killCancellationToken = default);

    /// <summary>
    /// Registers CrossGuard policy-pack directories to enforce inside subsequent plan previews.
    /// Only meaningful for the Pulumi engine; policies run inside the preview itself.
    /// </summary>
    void SetPolicyPacks(IReadOnlyList<string> packDirs);

    Task<string> Plan(
        Dictionary<string, string> parameters,
        string? planBeforeHook,
        string? planAfterHook,
        CancellationToken killCancellationToken = default);

    Task<string> PlanDestroy(
        Dictionary<string, string> parameters,
        string? beforeHook,
        string? afterHook,
        CancellationToken killCancellationToken = default);

    Task<string> ApplyFromPlan(
        string? beforeHook,
        string? afterHook,
        CancellationToken killCancellationToken = default);

    Task<string> DestroyFromPlan(
        string? beforeHook,
        string? afterHook,
        CancellationToken killCancellationToken = default);

    Task<string> Output(
        string? beforeHook,
        string? afterHook,
        CancellationToken killCancellationToken = default);

    Task<int> Statistics(
        CancellationToken killCancellationToken = default);

    /// <summary>
    /// How many destroyable resources the state holds. Answerable without input variables, which a
    /// destroy plan cannot be: the variables come from upstream outputs that may not exist. Zero
    /// covers both an empty state and one that was never written.
    /// </summary>
    Task<int> CountResourcesInState(
        CancellationToken killCancellationToken = default);

    Task<int> ReadStatisticsFromFile();

    IParsedPlan ParseApplyPlan();
    IParsedPlan ParseDestroyPlan();

    Task<OutputSetCreateDto?> ParseJsonToModuleOutputSet(
        string json, Dictionary<string, bool>? outputSources = null);
}

