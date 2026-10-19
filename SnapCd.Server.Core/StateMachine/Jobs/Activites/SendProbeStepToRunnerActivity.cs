// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Services.MaintenanceMode;
using SnapCd.Server.Core.Services.RippleProbes;

namespace SnapCd.Server.Core.StateMachine.Jobs.Activites;

/// <summary>
/// The probe step's request, carrying the inputs document built from the upstreams' signals. The
/// document is read here rather than held on the saga: it contains sensitive values, and the saga
/// row outlives the dispatch.
/// </summary>
public class SendProbeStepToRunnerActivity<TSaga, TMessage, TOutgoingMessage>(
    SnapCdDbContext dbContext,
    IMaintenanceModeService maintenanceMode,
    ProbeInputsBuilderFactory inputsFactory,
    ILogger<SendToRunnerActivity<TSaga, TMessage, TOutgoingMessage>> logger)
    : SendToRunnerActivity<TSaga, TMessage, TOutgoingMessage>(dbContext, maintenanceMode, logger)
    where TSaga : JobSagaBase
    where TMessage : class
    where TOutgoingMessage : StepRequestBase, new()
{
    protected override async Task Enrich(TOutgoingMessage message, TSaga saga)
    {
        if (message is not ProbeRequestedBase probe) return;

        using var inputs = inputsFactory.Create();

        var probeWaveId = await inputs.FindWaveToProbeFor(saga.ModuleId, saga.OrganizationId);
        if (probeWaveId == null) return;

        probe.ProbeWaveId = probeWaveId.Value;
        probe.InputsJson = await inputs.BuildInputsJson(
            probeWaveId.Value, saga.ModuleId, saga.OrganizationId) ?? string.Empty;
    }
}
