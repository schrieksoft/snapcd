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
/// Stores what a probe reported against the participant that ran it. Each value object is stored
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
    /// Stores what a probe reported and returns the participant it was recorded against, so the
    /// caller can resolve the edges leading out of it. Null when there was nothing to record.
    /// </summary>
    public async Task<Guid?> Record(Guid moduleId, Guid organizationId, Guid moduleJobId, string signalsJson)
    {
        var participant = await _dbContext.ProbeWaveParticipants
            .Where(p => p.ModuleId == moduleId && p.OrganizationId == organizationId)
            .OrderByDescending(p => p.CreatedDateTime)
            .FirstOrDefaultAsync();

        if (participant == null)
        {
            _logger.LogWarning(
                "Probe reported signals for Module {ModuleId} with no participant to record them against.",
                moduleId);
            return null;
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
            ProbeWaveId = participant.ProbeWaveId,
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

        participant.SignalOutputSetId = set.Id;
        participant.LastProbeModuleJobId = moduleJobId;

        await _dbContext.SaveChangesAsync();

        await _bus.Publish(new ProbeWaveUpdatedEvent
        {
            ProbeWaveId = participant.ProbeWaveId,
            OrganizationId = organizationId,
            ModuleId = moduleId
        });

        return participant.Id;
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
