// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.ProbeWaves;
using SnapCd.Server.Core.Entities.Definition.Signals;
using SnapCd.Server.Core.Events.System;

namespace SnapCd.Server.Core.Services.Nonsecured.RippleProbes;

public class ProbeSignalRecorderFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    ISignalSecretWriter secretWriter,
    IPublishEndpoint bus,
    ILoggerFactory loggerFactory)
{
    public ProbeSignalRecorder Create()
    {
        return new ProbeSignalRecorder(
            dbFactory.CreateDbContext(),
            secretWriter,
            bus,
            loggerFactory.CreateLogger<ProbeSignalRecorder>());
    }
}

/// <summary>
/// Stores what a probe reported against the Module that ran it. Each value object is stored
/// whole, because a downstream consumer's inputs document carries it verbatim: only the key
/// changes, from this Module's output name to the variable it feeds.
/// </summary>
public class ProbeSignalRecorder : IDisposable
{
    private readonly SnapCdDbContext _dbContext;
    private readonly ISignalSecretWriter _secretWriter;
    private readonly IPublishEndpoint _bus;
    private readonly ILogger<ProbeSignalRecorder> _logger;

    public ProbeSignalRecorder(
        SnapCdDbContext dbContext,
        ISignalSecretWriter secretWriter,
        IPublishEndpoint bus,
        ILogger<ProbeSignalRecorder> logger)
    {
        _dbContext = dbContext;
        _secretWriter = secretWriter;
        _bus = bus;
        _logger = logger;
    }

    /// <summary>
    /// Stores what a probe reported and returns the wave it was recorded against, so the caller
    /// can resolve the edges leading out of this Module. Null when there was nothing to record.
    /// </summary>
    /// <summary>
    /// Notes that this job is probing this Module for this wave, written just before the probe
    /// runs so an in-flight probe is visible. One row per probe: a Module can be probed again by
    /// a later apply, and each attempt keeps its own.
    /// </summary>
    public async Task RecordAttempt(Guid probeWaveId, Guid moduleId, Guid organizationId, Guid moduleJobId)
    {
        // An ask for another prediction already wrote the attempt that owes it, so this claims
        // that row for the job carrying the probe rather than adding a second one beside it.
        var claimed = await _dbContext.ModuleProbeAttempts
            .Where(a => a.ProbeWaveId == probeWaveId &&
                        a.ModuleId == moduleId &&
                        a.OrganizationId == organizationId &&
                        a.ProbeModuleJobId == null &&
                        a.SignalOutputSetId == null)
            .OrderByDescending(a => a.Timestamp)
            .FirstOrDefaultAsync();

        if (claimed != null)
        {
            claimed.ProbeModuleJobId = moduleJobId;
        }
        else
        {
            _dbContext.ModuleProbeAttempts.Add(new ModuleProbeAttempt
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                ProbeWaveId = probeWaveId,
                ModuleId = moduleId,
                ProbeModuleJobId = moduleJobId,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }

        await _dbContext.SaveChangesAsync();

        await _bus.Publish(new ProbeWaveUpdatedEvent
        {
            ProbeWaveId = probeWaveId,
            OrganizationId = organizationId,
            ModuleId = moduleId
        });
    }

    public async Task<Guid?> Record(
        Guid probeWaveId,
        Guid moduleId,
        Guid organizationId,
        Guid moduleJobId,
        string signalsJson)
    {
        // The reporting job's own attempt row, claimed before it started. The origin has none,
        // because it records its own plan's signals without a probe ever being claimed.
        var attempt = await _dbContext.ModuleProbeAttempts
            .FirstOrDefaultAsync(a => a.ProbeWaveId == probeWaveId &&
                                      a.ModuleId == moduleId &&
                                      a.OrganizationId == organizationId &&
                                      a.ProbeModuleJobId == moduleJobId);

        if (attempt == null)
        {
            attempt = new ModuleProbeAttempt
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                ProbeWaveId = probeWaveId,
                ModuleId = moduleId,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            _dbContext.ModuleProbeAttempts.Add(attempt);
        }

        JsonObject? outputs;
        JsonArray? resourceChanges;
        try
        {
            var document = JsonNode.Parse(signalsJson);
            outputs = document?["outputs"]?.AsObject();
            resourceChanges = document?["resource_changes"]?.AsArray();
        }
        catch (JsonException)
        {
            _logger.LogWarning("Probe for Module {ModuleId} returned a signals document that would not parse.", moduleId);
            return null;
        }

        if (outputs == null) return null;

        RecordResourceChanges(attempt, resourceChanges, moduleId, organizationId, probeWaveId, moduleJobId);

        var set = new SignalOutputSet
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = moduleId,
            ProbeWaveId = probeWaveId,
            ModuleJobId = moduleJobId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Checksum = Checksum(signalsJson)
        };

        _dbContext.SignalOutputSets.Add(set);

        foreach (var (name, value) in outputs)
        {
            if (value == null) continue;

            var json = value.ToJsonString();
            var sensitive = value["sensitive"]?.GetValue<bool>() ?? false;

            if (sensitive)
            {
                var remoteName = $"signal--{organizationId}--{set.Id}--{name}";
                await _secretWriter.Write(remoteName, json, organizationId);

                _dbContext.SecretSignalOutputs.Add(new SecretSignalOutput
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    SignalOutputSetId = set.Id,
                    Name = name,
                    Sensitive = true,
                    RemoteSecretName = remoteName
                });
            }
            else
            {
                _dbContext.LiteralSignalOutputs.Add(new LiteralSignalOutput
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    SignalOutputSetId = set.Id,
                    Name = name,
                    Sensitive = false,
                    SignalJson = json
                });
            }
        }

        attempt.SignalOutputSetId = set.Id;
        attempt.ProbeModuleJobId = moduleJobId;

        await _dbContext.SaveChangesAsync();

        await _bus.Publish(new ProbeWaveUpdatedEvent
        {
            ProbeWaveId = probeWaveId,
            OrganizationId = organizationId,
            ModuleId = moduleId
        });

        return probeWaveId;
    }

    /// <summary>
    /// How many predicted resource changes are stored for one probe. A wide blast radius is
    /// reported by its count rather than row by row, so one probe cannot fill the table.
    /// </summary>
    private const int MaxStoredResourceChanges = 500;

    /// <summary>
    /// Stores what the probe predicted for this Module's own resources. Absent for an engine whose
    /// probe does not report them, and empty where the probe predicted none, which is a finding in
    /// its own right: it is what distinguishes a Module that changes nothing from one whose outputs
    /// merely hold still.
    /// </summary>
    private void RecordResourceChanges(
        ModuleProbeAttempt attempt,
        JsonArray? resourceChanges,
        Guid moduleId,
        Guid organizationId,
        Guid probeWaveId,
        Guid moduleJobId)
    {
        if (resourceChanges == null) return;

        var set = new ResourceChangeSet
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = moduleId,
            ProbeWaveId = probeWaveId,
            ModuleJobId = moduleJobId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            TotalCount = resourceChanges.Count
        };

        _dbContext.ResourceChangeSets.Add(set);

        var stored = 0;
        var seen = new HashSet<string>();

        foreach (var change in resourceChanges)
        {
            if (stored >= MaxStoredResourceChanges) break;
            if (change == null) continue;

            var address = change["address"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(address)) continue;

            // The address is unique within a plan, and the unique index says so.
            if (!seen.Add(address)) continue;

            var actions = change["action"]?.AsArray();

            _dbContext.ResourceChanges.Add(new ResourceChange
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                ResourceChangeSetId = set.Id,
                Address = Truncate(address, 1024),
                ResourceType = Truncate(change["type"]?.GetValue<string>() ?? string.Empty, 255),
                Name = Truncate(change["name"]?.GetValue<string>() ?? string.Empty, 255),
                Actions = Truncate(
                    actions == null
                        ? string.Empty
                        : string.Join(",", actions.Select(a => a?.GetValue<string>()).Where(a => a != null)),
                    100)
            });

            stored++;
        }

        if (set.TotalCount > stored)
        {
            _logger.LogDebug(
                "Probe for Module {ModuleId} predicted {Total} resource changes; storing the first {Stored}.",
                moduleId, set.TotalCount, stored);
        }

        attempt.ResourceChangeSetId = set.Id;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];

    private static string Checksum(string document)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(document)));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
