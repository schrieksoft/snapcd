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
using SnapCd.Server.Core.Events.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.ProbeWaves;
using SnapCd.Server.Core.Entities.Definition.Signals;

namespace SnapCd.Server.Core.Services.RippleProbes;

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
        _dbContext.ModuleProbeAttempts.Add(new ModuleProbeAttempt
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProbeWaveId = probeWaveId,
            ModuleId = moduleId,
            ProbeModuleJobId = moduleJobId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });

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
        try
        {
            outputs = JsonNode.Parse(signalsJson)?["outputs"]?.AsObject();
        }
        catch (JsonException)
        {
            _logger.LogWarning("Probe for Module {ModuleId} returned a signals document that would not parse.", moduleId);
            return null;
        }

        if (outputs == null) return null;

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

    private static string Checksum(string document)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(document)));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
