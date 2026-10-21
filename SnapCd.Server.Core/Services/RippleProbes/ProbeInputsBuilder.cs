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
using Microsoft.Extensions.Logging;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.Signals;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Services.RippleProbes;

public class ProbeInputsBuilderFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    RippleProbeConsumerLookupFactory lookupFactory,
    ISignalSecretReader secretReader,
    ILoggerFactory loggerFactory)
{
    public ProbeInputsBuilder Create()
    {
        return new ProbeInputsBuilder(
            dbFactory.CreateDbContext(),
            lookupFactory.Create(),
            secretReader,
            loggerFactory.CreateLogger<ProbeInputsBuilder>());
    }
}

/// <summary>
/// Whether a Module has anything to probe for in a wave, and the inputs document to probe
/// with. The document is the signal value objects copied verbatim under a variables key, keyed by
/// the consumer's own variable names rather than the producers' output names.
/// </summary>
public class ProbeInputsBuilder : IDisposable
{
    /// <summary>planwalker rejects a document whose format_version it does not know.</summary>
    public const string SignalsFormatVersion = "1";

    private readonly SnapCdDbContext _dbContext;
    private readonly RippleProbeConsumerLookup _lookup;
    private readonly ISignalSecretReader _secretReader;
    private readonly ILogger<ProbeInputsBuilder> _logger;

    public ProbeInputsBuilder(
        SnapCdDbContext dbContext,
        RippleProbeConsumerLookup lookup,
        ISignalSecretReader secretReader,
        ILogger<ProbeInputsBuilder> logger)
    {
        _dbContext = dbContext;
        _lookup = lookup;
        _secretReader = secretReader;
        _logger = logger;
    }

    /// <summary>
    /// Every wave this Module still owes a probe to.
    ///
    /// A wave it has already reported in is not its business any more, which is what stops a
    /// Module probing again on every later apply. Several waves can be open on one Module at
    /// once, from different origins, and each predicts different values for its inputs, so each
    /// is owed its own probe rather than one answer shared between them.
    ///
    /// <paramref name="exceptAttemptedByJob"/> also drops the waves that job has already tried,
    /// answered or not: a faulted probe records no signals, so the wave it tried stays owed on
    /// the signal test alone.
    /// </summary>
    public async Task<List<Guid>> FindWavesToProbeFor(
        Guid moduleId,
        Guid organizationId,
        Guid? exceptAttemptedByJob = null)
    {
        var settled = await _dbContext.ModuleProbeAttempts
            .AsNoTracking()
            .Where(a => a.ModuleId == moduleId &&
                        a.OrganizationId == organizationId &&
                        (a.SignalOutputSetId != null ||
                         (exceptAttemptedByJob != null && a.ProbeModuleJobId == exceptAttemptedByJob)))
            .Select(a => a.ProbeWaveId)
            .ToListAsync();

        return await _dbContext.SignalEdges
            .AsNoTracking()
            .Where(e =>
                e.ConsumerModuleId == moduleId &&
                e.OrganizationId == organizationId &&
                e.Status == SignalEdgeStatus.Confirmed &&
                !settled.Contains(e.ProbeWaveId))
            .Select(e => e.ProbeWaveId)
            .Distinct()
            .ToListAsync();
    }

    /// <summary>
    /// The inputs document for a Module's probe in one wave, or null when its confirmed upstreams
    /// turn out to carry no signal this Module reads.
    /// </summary>
    public async Task<string?> BuildInputsJson(Guid probeWaveId, Guid moduleId, Guid organizationId)
    {
        var upstreamSetIds = await _dbContext.SignalEdges
            .Where(e =>
                e.ProbeWaveId == probeWaveId &&
                e.ConsumerModuleId == moduleId &&
                e.OrganizationId == organizationId &&
                e.Status == SignalEdgeStatus.Confirmed)
            .Join(
                _dbContext.ModuleProbeAttempts.Where(a => a.ProbeWaveId == probeWaveId),
                e => e.UpstreamModuleId,
                a => a.ModuleId,
                (e, a) => new { a.ModuleId, a.SignalOutputSetId })
            .Where(x => x.SignalOutputSetId != null)
            // Ordered so a collision resolves the same way every run: an unordered result would
            // make the winning value depend on the query plan.
            .OrderBy(x => x.ModuleId)
            .ToListAsync();

        if (upstreamSetIds.Count == 0) return null;

        var variables = new JsonObject();

        foreach (var upstream in upstreamSetIds)
        {
            var signals = await _dbContext.SignalOutputs
                .Where(s => s.SignalOutputSetId == upstream.SignalOutputSetId!.Value && s.OrganizationId == organizationId)
                .OrderBy(s => s.Name)
                .ToListAsync();

            if (signals.Count == 0) continue;

            var consumers = await _lookup.ListConsumers(
                upstream.ModuleId,
                organizationId,
                signals.Select(s => s.Name).ToList());

            var edges = consumers.FirstOrDefault(c => c.ModuleId == moduleId)?.VariableNamesByOutputName;
            if (edges == null) continue;

            foreach (var signal in signals)
            {
                if (!edges.TryGetValue(signal.Name, out var variableName)) continue;

                var value = await ReadSignalObject(signal, organizationId);
                if (value == null) continue;

                if (variables.ContainsKey(variableName))
                {
                    _logger.LogWarning(
                        "Probe for Module {ModuleId}: variable {VariableName} is fed by more than one upstream in this wave, keeping the first. Upstream {UpstreamModuleId} supplied output {OutputName}.",
                        moduleId, variableName, upstream.ModuleId, signal.Name);
                    continue;
                }

                variables[variableName] = value;
            }
        }

        if (variables.Count == 0) return null;

        return new JsonObject
        {
            ["format_version"] = SignalsFormatVersion,
            ["variables"] = variables
        }.ToJsonString();
    }

    private async Task<JsonNode?> ReadSignalObject(SignalOutput signal, Guid organizationId)
    {
        var json = signal switch
        {
            LiteralSignalOutput literal => literal.SignalJson,
            SecretSignalOutput secret => await _secretReader.Read(secret, organizationId),
            _ => null
        };

        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            return JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            _logger.LogWarning("Probe: signal {Name} did not hold a readable value object, skipping it.", signal.Name);
            return null;
        }
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _lookup.Dispose();
    }
}
