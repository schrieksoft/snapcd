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

    /// <summary>Every wave a Module takes part in, whether or not it started them.</summary>
    public async Task<List<ProbeWaveView>> ListByParticipatingModule(
        Guid moduleId,
        Guid organizationId,
        Func<Guid, bool>? canReadModule = null,
        int take = 20)
    {
        var waveIds = await _dbContext.ProbeWaveParticipants
            .Where(p => p.ModuleId == moduleId && p.OrganizationId == organizationId)
            .Select(p => p.ProbeWaveId)
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
                w.MaxDepth,
                w.CreatedDateTime,
                OriginModuleName = w.OriginModule.Name
            })
            .FirstOrDefaultAsync();

        if (wave == null) return null;

        var participants = await _dbContext.ProbeWaveParticipants
            .AsNoTracking()
            .Where(p => p.ProbeWaveId == waveId && p.OrganizationId == organizationId)
            .Select(p => new
            {
                p.Id,
                p.ModuleId,
                p.SignalOutputSetId,
                p.LastProbeModuleJobId,
                ModuleName = p.Module.Name,
                NamespaceName = p.Module.Namespace.Name
            })
            .ToListAsync();

        var visible = canReadModule == null
            ? participants
            : participants.Where(p => canReadModule(p.ModuleId)).ToList();

        var view = new ProbeWaveView
        {
            Id = wave.Id,
            OriginModuleId = wave.OriginModuleId,
            OriginModuleName = wave.OriginModuleName,
            OriginModuleJobId = wave.OriginModuleJobId,
            MaxDepth = wave.MaxDepth,
            CreatedDateTime = wave.CreatedDateTime,
            ParticipantsOmitted = participants.Count - visible.Count
        };

        var visibleIds = visible.Select(p => p.Id).ToList();

        var contributions = await _dbContext.SignalContributions
            .AsNoTracking()
            .Where(c => visibleIds.Contains(c.ProbeWaveParticipantId) && c.OrganizationId == organizationId)
            .Select(c => new
            {
                c.ProbeWaveParticipantId,
                c.UpstreamParticipantId,
                c.Status,
                UpstreamModuleId = c.UpstreamParticipant.ModuleId,
                UpstreamModuleName = c.UpstreamParticipant.Module.Name
            })
            .ToListAsync();

        var setIds = visible.Where(p => p.SignalOutputSetId != null)
            .Select(p => p.SignalOutputSetId!.Value)
            .ToList();

        var signals = await _dbContext.SignalOutputs
            .AsNoTracking()
            .Where(s => setIds.Contains(s.SignalOutputSetId) && s.OrganizationId == organizationId)
            .OrderBy(s => s.Name)
            .ToListAsync();

        foreach (var p in visible.OrderBy(p => p.NamespaceName).ThenBy(p => p.ModuleName))
        {
            view.Participants.Add(new ProbeWaveParticipantView
            {
                Id = p.Id,
                ModuleId = p.ModuleId,
                ModuleName = p.ModuleName,
                NamespaceName = p.NamespaceName,
                LastProbeModuleJobId = p.LastProbeModuleJobId,
                HasProbed = p.SignalOutputSetId != null,
                Contributions = contributions
                    .Where(c => c.ProbeWaveParticipantId == p.Id)
                    .OrderBy(c => c.UpstreamModuleName)
                    .Select(c => new SignalContributionView
                    {
                        UpstreamParticipantId = c.UpstreamParticipantId,
                        UpstreamModuleId = c.UpstreamModuleId,
                        UpstreamModuleName = c.UpstreamModuleName,
                        Status = c.Status
                    })
                    .ToList(),
                Signals = p.SignalOutputSetId == null
                    ? []
                    : signals
                        .Where(s => s.SignalOutputSetId == p.SignalOutputSetId!.Value)
                        .Select(Describe)
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

            view.Action = value["action"]?.GetValue<string>();
            view.AfterIsUnknown = view.Action == "unknown";
            view.HasBefore = value["before"] != null;
        }
        catch (JsonException)
        {
            // A signal that will not parse is reported as a signal with no detail rather than
            // failing the whole wave's view.
        }

        return view;
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
