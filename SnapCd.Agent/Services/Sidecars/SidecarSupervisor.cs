// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Agent.Services.Sidecars;

/// <summary>
/// Periodically health-checks each registered sidecar and logs unhealthy ones. In v1 the sidecar
/// process lifecycle is owned by the container runtime (docker-compose restart policy), so this is
/// observation + logging rather than process restart; process supervision is a follow-up.
/// </summary>
public sealed class SidecarSupervisor : BackgroundService
{
    private readonly SidecarRegistry _registry;
    private readonly ILogger<SidecarSupervisor> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    public SidecarSupervisor(SidecarRegistry registry, ILogger<SidecarSupervisor> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var sidecar in _registry.All)
            {
                var healthy = await sidecar.IsHealthyAsync(stoppingToken);
                if (!healthy)
                    _logger.LogWarning("Sidecar '{Sidecar}' is unhealthy.", sidecar.Name);
            }
        }
    }
}
