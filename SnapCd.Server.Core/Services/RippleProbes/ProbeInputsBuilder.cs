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
/// Whether a Module's participant has anything to probe for, and the inputs document to probe
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
    /// The participant this Module probes for in the wave it is taking part in, or null when it has
    /// no confirmed contribution and the step is skipped.
    /// </summary>
    public async Task<Guid?> FindParticipantToProbeFor(Guid moduleId, Guid organizationId)
    {
        var participantIds = await _dbContext.ProbeWaveParticipants
            .Where(p =>
                p.ModuleId == moduleId &&
                p.OrganizationId == organizationId &&
                p.SignalOutputSetId == null)
            .Select(p => p.Id)
            .ToListAsync();

        if (participantIds.Count == 0) return null;

        return await _dbContext.SignalContributions
            .Where(c =>
                participantIds.Contains(c.ProbeWaveParticipantId) &&
                c.OrganizationId == organizationId &&
                c.Status == SignalContributionStatus.Confirmed)
            .Select(c => (Guid?)c.ProbeWaveParticipantId)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// The inputs document for a participant, or null when its confirmed upstreams turn out to
    /// carry no signal this Module reads.
    /// </summary>
    public async Task<string?> BuildInputsJson(Guid participantId, Guid moduleId, Guid organizationId)
    {
        var upstreamSetIds = await _dbContext.SignalContributions
            .Where(c =>
                c.ProbeWaveParticipantId == participantId &&
                c.OrganizationId == organizationId &&
                c.Status == SignalContributionStatus.Confirmed)
            .Join(
                _dbContext.ProbeWaveParticipants,
                c => c.UpstreamParticipantId,
                p => p.Id,
                (c, p) => new { p.ModuleId, p.SignalOutputSetId })
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
