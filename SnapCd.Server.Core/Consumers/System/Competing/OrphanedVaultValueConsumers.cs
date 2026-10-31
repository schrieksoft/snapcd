// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Services.Nonsecured.Vaults;
using SnapCd.Server.Core.Services.Utils.Vaults;

namespace SnapCd.Server.Core.Consumers.System.Competing;

/// <summary>
/// Deleting a Stack, Namespace or Module is a SQL cascade that makes no vault call, so its
/// Secrets' values outlive their rows. These consumers remove them once the delete has committed.
///
/// They run after the rows are gone, which is the order that matters: a sweep that ran first and
/// then saw the delete fail would have destroyed values belonging to something that still exists.
/// </summary>
public class StackDeletedVaultConsumer(OrphanedVaultValueSweeper sweeper)
    : IConsumer<StackDeletedEvent>
{
    public async Task Consume(ConsumeContext<StackDeletedEvent> context)
    {
        var organizationId = context.Message.OrganizationId;

        await sweeper.SweepPrefix(
            RemoteSecretName.OwnerPrefix(SecretScope.Stack, organizationId, context.Message.Data.Id),
            organizationId,
            VaultKind.Input,
            context.CancellationToken);
    }
}

public class NamespaceDeletedVaultConsumer(OrphanedVaultValueSweeper sweeper)
    : IConsumer<NamespaceDeletedEvent>
{
    public async Task Consume(ConsumeContext<NamespaceDeletedEvent> context)
    {
        var organizationId = context.Message.OrganizationId;

        await sweeper.SweepPrefix(
            RemoteSecretName.OwnerPrefix(SecretScope.Namespace, organizationId, context.Message.Data.Id),
            organizationId,
            VaultKind.Input,
            context.CancellationToken);
    }
}

/// <summary>
/// A Module owns three kinds of value: its Secrets, the sensitive outputs of every OutputSet it
/// produced, and the sensitive probe signals recorded against it. Outputs live in the output
/// vault; Secrets and signals in the input one.
/// </summary>
public class ModuleDeletedVaultConsumer(OrphanedVaultValueSweeper sweeper)
    : IConsumer<ModuleDeletedEvent>
{
    public async Task Consume(ConsumeContext<ModuleDeletedEvent> context)
    {
        var organizationId = context.Message.OrganizationId;
        var moduleId = context.Message.Data.Id;

        await sweeper.SweepPrefix(
            RemoteSecretName.OwnerPrefix(SecretScope.Module, organizationId, moduleId),
            organizationId,
            VaultKind.Input,
            context.CancellationToken);

        await sweeper.SweepPrefix(
            RemoteSecretName.OutputPrefix(organizationId, moduleId),
            organizationId,
            VaultKind.Output,
            context.CancellationToken);
    }
}

/// <summary>
/// An OutputSet can be deleted on its own from the Module's outputs page.
///
/// What its sensitive outputs were stored under is shared: an output's vault name carries the
/// Module and a hash of the output's name, not the set, so every set of that Module that declared
/// the same output points at the same value. Deleting this set's values would take the surviving
/// sets' values with them.
///
/// So a value is removed only once no set of that Module still declares that output. The set's own
/// rows are gone by the time this runs, which is what makes the question answerable: whatever the
/// Module still has is what must be kept.
/// </summary>
public class OutputSetDeletedVaultConsumer(OrphanedVaultValueSweeper sweeper)
    : IConsumer<OutputSetDeletedEvent>
{
    public async Task Consume(ConsumeContext<OutputSetDeletedEvent> context)
    {
        await sweeper.SweepUnreferencedModuleOutputs(
            context.Message.Data.ModuleId,
            context.Message.OrganizationId,
            context.CancellationToken);
    }
}

/// <summary>
/// An Integration's connection blob is keyed by the Integration's own id, so its name is already
/// all that is needed to find it.
/// </summary>
public class IntegrationDeletedVaultConsumer(OrphanedVaultValueSweeper sweeper)
    : IConsumer<IntegrationDeletedEvent>
{
    public async Task Consume(ConsumeContext<IntegrationDeletedEvent> context)
    {
        var organizationId = context.Message.OrganizationId;

        await sweeper.SweepPrefix(
            RemoteSecretName.IntegrationPrefix(organizationId, context.Message.Data.Id),
            organizationId,
            VaultKind.Input,
            context.CancellationToken);
    }
}

/// <summary>
/// A SignalOutputSet's sensitive signals are stored under names carrying the Module, not the set,
/// so sets of one Module share names where they answer the same wave. Only what no surviving set
/// still records is removed.
/// </summary>
public class SignalOutputSetDeletedVaultConsumer(OrphanedVaultValueSweeper sweeper)
    : IConsumer<SignalOutputSetDeletedEvent>
{
    public async Task Consume(ConsumeContext<SignalOutputSetDeletedEvent> context)
    {
        await sweeper.SweepUnreferencedSignals(
            context.Message.Data.ModuleId,
            context.Message.OrganizationId,
            context.CancellationToken);
    }
}
