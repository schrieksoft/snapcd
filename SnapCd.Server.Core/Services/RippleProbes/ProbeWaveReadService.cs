// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.Signals;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Views;

namespace SnapCd.Server.Core.Services.RippleProbes;

public class ProbeWaveReadServiceFactory(IDbContextFactory<SnapCdDbContext> dbContextFactory)
{
    public ProbeWaveReadService Create()
    {
        return new ProbeWaveReadService(dbContextFactory.CreateDbContext());
    }
}

/// <summary>
/// Reads a wave across its four tables for display.
///
/// One service rather than a repository per table: a wave is written by the wave machinery and read
/// by one view, so it has no CRUD, no DTOs and no quota behind it, and every question about it is a
/// join. <see cref="DependencyGraph.DependencyGraphService"/> is the same shape.
///
/// Every read is scoped by organization. Whether a reader may see a given Module is the caller's
/// question, passed in as a predicate: the page that shows a wave is already behind an
/// authorization gate, and this service has no principal of its own.
/// </summary>
public class ProbeWaveReadService : IDisposable
{
    private readonly SnapCdDbContext _dbContext;

    public ProbeWaveReadService(SnapCdDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>Waves started from a Module, newest first.</summary>
    public async Task<List<ProbeWaveView>> ListByOriginModule(
        Guid originModuleId,
        Guid organizationId,
        Func<Guid, bool>? canReadModule = null,
        int take = 20)
    {
        var waveIds = await _dbContext.ProbeWaves
            .Where(w => w.OriginModuleId == originModuleId && w.OrganizationId == organizationId)
            .OrderByDescending(w => w.CreatedDateTime)
            .Take(take)
            .Select(w => w.Id)
            .ToListAsync();

        var waves = new List<ProbeWaveView>();
        foreach (var id in waveIds)
        {
            var wave = await Get(id, organizationId, canReadModule);
            if (wave != null) waves.Add(wave);
        }

        return waves;
    }

    /// <summary>
    /// The wave a job's own plan started, or null when that plan changed nothing anyone reads.
    ///
    /// Keyed on the job rather than the Module because a Module has many jobs and only this one's
    /// findings belong beside this one's approval decision.
    /// </summary>
    public async Task<ProbeWaveView?> GetByOriginJob(
        Guid originModuleJobId,
        Guid organizationId,
        Func<Guid, bool>? canReadModule = null)
    {
        var waveId = await _dbContext.ProbeWaves
            .Where(w => w.OriginModuleJobId == originModuleJobId && w.OrganizationId == organizationId)
            .OrderByDescending(w => w.CreatedDateTime)
            .Select(w => w.Id)
            .FirstOrDefaultAsync();

        return waveId == Guid.Empty
            ? null
            : await Get(waveId, organizationId, canReadModule);
    }

    /// <summary>Every wave a Module takes part in, whether or not it started them.</summary>
    public async Task<List<ProbeWaveView>> ListByParticipatingModule(
        Guid moduleId,
        Guid organizationId,
        Func<Guid, bool>? canReadModule = null,
        int take = 20)
    {
        // A Module takes part in a wave because an edge points at it.
        var waveIds = await _dbContext.SignalEdges
            .Where(e => e.ConsumerModuleId == moduleId && e.OrganizationId == organizationId)
            .Select(e => e.ProbeWaveId)
            .Distinct()
            .ToListAsync();

        var ordered = await _dbContext.ProbeWaves
            .Where(w => waveIds.Contains(w.Id) && w.OrganizationId == organizationId)
            .OrderByDescending(w => w.CreatedDateTime)
            .Take(take)
            .Select(w => w.Id)
            .ToListAsync();

        var waves = new List<ProbeWaveView>();
        foreach (var id in ordered)
        {
            var wave = await Get(id, organizationId, canReadModule);
            if (wave != null) waves.Add(wave);
        }

        return waves;
    }

    /// <summary>
    /// One wave with its participants, their edges and their signals, or null when no such wave
    /// exists in this organization.
    ///
    /// A participant whose Module the reader cannot see is left out and counted. Refusing the whole
    /// wave for one unreadable Module would hide the rest of a wave the reader is entitled to.
    /// </summary>
    public async Task<ProbeWaveView?> Get(
        Guid waveId,
        Guid organizationId,
        Func<Guid, bool>? canReadModule = null)
    {
        var wave = await _dbContext.ProbeWaves
            .AsNoTracking()
            .Where(w => w.Id == waveId && w.OrganizationId == organizationId)
            .Select(w => new
            {
                w.Id,
                w.OriginModuleId,
                w.OriginModuleJobId,
                w.CreatedDateTime,
                OriginModuleName = w.OriginModule.Name
            })
            .FirstOrDefaultAsync();

        if (wave == null) return null;

        // The wave's membership is its edges: a Module is in it because something points at it.
        var edges = await _dbContext.SignalEdges
            .AsNoTracking()
            .Where(e => e.ProbeWaveId == waveId && e.OrganizationId == organizationId)
            .Select(e => new
            {
                e.ConsumerModuleId,
                e.UpstreamModuleId,
                e.Status,
                ConsumerModuleName = e.ConsumerModule.Name,
                ConsumerNamespaceName = e.ConsumerModule.Namespace.Name,
                UpstreamModuleName = e.UpstreamModule.Name
            })
            .ToListAsync();

        // Every attempt, newest first. A probe is repeatable, so a Module owing a wave across
        // several Applies has one row per run and a reader wants all of them.
        var attempts = (await _dbContext.ModuleProbeAttempts
                .AsNoTracking()
                .Where(a => a.ProbeWaveId == waveId && a.OrganizationId == organizationId)
                .Select(a => new { a.ModuleId, a.SignalOutputSetId, a.ProbeModuleJobId, a.Timestamp })
                .ToListAsync())
            .OrderByDescending(a => a.Timestamp)
            .ToList();

        // The attempt row keeps no status, so how a probe ended is the probe job's own status.
        var probeJobIds = attempts
            .Where(a => a.ProbeModuleJobId != null)
            .Select(a => a.ProbeModuleJobId!.Value)
            .ToList();

        var probeJobStatuses = probeJobIds.Count == 0
            ? new Dictionary<Guid, ExecutionStatus>()
            : await _dbContext.ModuleJobs
                .AsNoTracking()
                .Where(j => probeJobIds.Contains(j.Id) && j.OrganizationId == organizationId)
                .ToDictionaryAsync(j => j.Id, j => j.Status);

        var moduleIds = edges.Select(e => e.ConsumerModuleId).Distinct().ToList();

        var visibleIds = canReadModule == null
            ? moduleIds
            : moduleIds.Where(canReadModule).ToList();

        var view = new ProbeWaveView
        {
            Id = wave.Id,
            OriginModuleId = wave.OriginModuleId,
            OriginModuleName = wave.OriginModuleName,
            OriginModuleJobId = wave.OriginModuleJobId,
            CreatedDateTime = wave.CreatedDateTime,
            ModulesOmitted = moduleIds.Count - visibleIds.Count
        };

        var setIds = attempts
            .Where(a => a.SignalOutputSetId != null && visibleIds.Contains(a.ModuleId))
            .Select(a => a.SignalOutputSetId!.Value)
            .Distinct()
            .ToList();

        var signals = await _dbContext.SignalOutputs
            .AsNoTracking()
            .Where(s => setIds.Contains(s.SignalOutputSetId) && s.OrganizationId == organizationId)
            .OrderBy(s => s.Name)
            .ToListAsync();

        var ordered = visibleIds
            .Select(id => edges.First(e => e.ConsumerModuleId == id))
            .OrderBy(e => e.ConsumerNamespaceName)
            .ThenBy(e => e.ConsumerModuleName);

        foreach (var module in ordered)
        {
            view.Modules.Add(new ProbeWaveModuleView
            {
                ModuleId = module.ConsumerModuleId,
                ModuleName = module.ConsumerModuleName,
                NamespaceName = module.ConsumerNamespaceName,
                Attempts = attempts
                    .Where(a => a.ModuleId == module.ConsumerModuleId)
                    .Select(a => new ProbeAttemptView
                    {
                        ProbeModuleJobId = a.ProbeModuleJobId,
                        Timestamp = a.Timestamp,
                        HasProbed = a.SignalOutputSetId != null,
                        ProbeJobStatus = a.ProbeModuleJobId is { } probeJobId &&
                                         probeJobStatuses.TryGetValue(probeJobId, out var probeStatus)
                            ? probeStatus
                            : null,
                        Signals = a.SignalOutputSetId == null
                            ? []
                            : signals
                                .Where(x => x.SignalOutputSetId == a.SignalOutputSetId!.Value)
                                .Select(Describe)
                                .ToList()
                    })
                    .ToList(),
                Edges = edges
                    .Where(e => e.ConsumerModuleId == module.ConsumerModuleId)
                    .OrderBy(e => e.UpstreamModuleName)
                    .Select(e => new SignalEdgeView
                    {
                        UpstreamModuleId = e.UpstreamModuleId,
                        UpstreamModuleName = e.UpstreamModuleName,
                        Status = e.Status
                    })
                    .ToList()
            });
        }

        return view;
    }

    /// <summary>
    /// What a stored signal says, without decoding its value. planwalker sets the action to
    /// unknown exactly when the plan cannot know the new value, so the finding is read off that
    /// field rather than by inspecting the encoded bytes.
    /// </summary>
    private static SignalOutputView Describe(SignalOutput signal)
    {
        var view = new SignalOutputView
        {
            Name = signal.Name,
            Sensitive = signal.Sensitive
        };

        // A sensitive signal's object lives in the secret store, so there is nothing to read here
        // and nothing that should be shown if there were.
        if (signal is not LiteralSignalOutput literal) return view;

        try
        {
            var value = JsonNode.Parse(literal.SignalJson)?.AsObject();
            if (value == null) return view;

            view.Actions = ActionList(value["action"]);
            view.AfterIsUnknown = IsUnknown(value["after"]);
            view.HasBefore = value["before"] != null;
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException)
        {
            // A signal that will not parse is reported as a signal with no detail rather than
            // failing the whole wave's view.
        }

        return view;
    }

    /// <summary>
    /// The plan's actions, in the form "tofu show -json" writes them: one element for a simple
    /// action, two for a replace.
    /// </summary>
    private static List<string> ActionList(JsonNode? action)
    {
        var parts = action?.AsArray();
        if (parts == null) return [];

        return parts.Select(p => p?.GetValue<string>()).OfType<string>().ToList();
    }

    /// <summary>
    /// Whether the plan could not predict the new value. Unknown-ness is a property of the value
    /// rather than of the action: an output can be updated to a value the plan knows.
    /// </summary>
    private static bool IsUnknown(JsonNode? after) =>
        after is JsonObject o && o["$unknown"]?.GetValue<bool>() == true;

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
