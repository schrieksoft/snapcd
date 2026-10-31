// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Services.Utils;

/// <summary>
/// Clears the rows that would otherwise refuse the delete of a Stack, a Namespace, a Module or a
/// Module job.
///
/// SQL Server allows one cascade path into a table, so wherever something reaches a table twice
/// over the second path has to restrict, and a restricted row then refuses the delete it should
/// have gone with. Two kinds end up here: the rows that restrict on the thing being deleted, and
/// the rows that restrict on one of those.
///
/// An owner further up deletes the ones below it by cascade, so a Stack, a Namespace and a Module
/// all need the same Module-scoped rows gone and differ only in which Modules they mean. The
/// predicate is the only difference.
///
/// Order within each method matters: a referencing row goes before the row it points at.
/// </summary>
public static class DeleteBlockerCleanup
{
    /// <summary>
    /// Clears what would refuse the delete of these Namespaces.
    ///
    /// A Stack and a Namespace both delete Namespaces, the first by cascade, so both need these
    /// rows gone and differ only in which Namespaces they mean.
    /// </summary>
    /// <param name="namespaces">Which Namespaces are being deleted: by id, or by stack.</param>
    public static async Task ClearRowsBlockingNamespaceDelete(
        this SnapCdDbContext dbContext,
        Expression<Func<Namespace, bool>> namespaces,
        Guid organizationId)
    {
        var namespaceIds = await dbContext.Namespaces
            .Where(namespaces)
            .Where(n => n.OrganizationId == organizationId)
            .Select(n => n.Id)
            .ToListAsync();

        if (namespaceIds.Count == 0) return;

        // A namespace secret is restricted from the namespace that scopes it, and is itself
        // restricted from the parameters and variables that read it, so the readers go first.
        await dbContext.NamespaceParamFromSecrets
            .Where(x => x.OrganizationId == organizationId && namespaceIds.Contains(x.NamespaceId))
            .ExecuteDeleteAsync();

        await dbContext.NamespaceEnvVarFromSecrets
            .Where(x => x.OrganizationId == organizationId && namespaceIds.Contains(x.NamespaceId))
            .ExecuteDeleteAsync();

        await dbContext.NamespaceSecrets
            .Where(x => x.OrganizationId == organizationId && namespaceIds.Contains(x.NamespaceId))
            .ExecuteDeleteAsync();
    }

    /// <summary>
    /// Clears what would refuse the delete of this Module job.
    ///
    /// A job owns at most one wave, which cascades with it, but its probe can also answer another
    /// Module's wave: the attempt and the two sets then hang off a wave that survives, naming the
    /// job in a column that cannot cascade for the same one-path reason.
    /// </summary>
    public static async Task ClearRowsBlockingModuleJobDelete(
        this SnapCdDbContext dbContext, Guid moduleJobId, Guid organizationId)
    {
        // A runner assignment records which runner took the job and restricts on it.
        await dbContext.JobRunnerAssignments
            .Where(x => x.OrganizationId == organizationId && x.JobId == moduleJobId)
            .ExecuteDeleteAsync();

        // The attempt points at both sets, so it goes before them.
        await dbContext.ModuleProbeAttempts
            .Where(a => a.OrganizationId == organizationId && a.ProbeModuleJobId == moduleJobId)
            .ExecuteDeleteAsync();

        await dbContext.SignalOutputSets
            .Where(x => x.OrganizationId == organizationId && x.ModuleJobId == moduleJobId)
            .ExecuteDeleteAsync();

        await dbContext.ResourceChangeSets
            .Where(x => x.OrganizationId == organizationId && x.ModuleJobId == moduleJobId)
            .ExecuteDeleteAsync();
    }

    /// <summary>
    /// Clears what would refuse the delete of these Modules. A secret's readers are included,
    /// because they restrict on the secret rather than on the Module.
    /// </summary>
    /// <param name="modules">Which Modules are being deleted: by id, by namespace, or by stack.</param>
    public static async Task ClearRowsBlockingModuleDelete(
        this SnapCdDbContext dbContext,
        Expression<Func<Module, bool>> modules,
        Guid organizationId)
    {
        var moduleIds = await dbContext.Modules
            .Where(modules)
            .Where(m => m.OrganizationId == organizationId)
            .Select(m => m.Id)
            .ToListAsync();

        if (moduleIds.Count == 0) return;

        // A secret is restricted from the parameters and variables that read it, and from the
        // Module that scopes it, so the readers go first.
        await dbContext.ModuleParamFromSecrets
            .Where(x => x.OrganizationId == organizationId && moduleIds.Contains(x.ModuleId))
            .ExecuteDeleteAsync();

        await dbContext.ModuleEnvVarFromSecrets
            .Where(x => x.OrganizationId == organizationId && moduleIds.Contains(x.ModuleId))
            .ExecuteDeleteAsync();

        await dbContext.ModuleSecrets
            .Where(x => x.OrganizationId == organizationId && moduleIds.Contains(x.ModuleId))
            .ExecuteDeleteAsync();

        // A consumer's parameter or variable that reads a doomed Module's output restricts on that
        // Module, where its own Module cascades, so it is matched from the upstream side. The
        // consumer keeps the row's absence rather than a reference to nothing: an output parameter
        // whose upstream has no output set is already skipped when inputs are resolved.
        //
        // Deleting a Module is the owner's to take, and a row another Module's owner added would
        // otherwise block it with nothing the upstream owner has the rights to remove.
        // Each concrete type, because the class they share is not itself mapped.
        await dbContext.ModuleParamFromOutputs
            .Where(x => x.OrganizationId == organizationId && moduleIds.Contains(x.OutputModuleId))
            .ExecuteDeleteAsync();

        await dbContext.ModuleEnvVarFromOutputs
            .Where(x => x.OrganizationId == organizationId && moduleIds.Contains(x.OutputModuleId))
            .ExecuteDeleteAsync();

        await dbContext.ModuleParamFromOutputSets
            .Where(x => x.OrganizationId == organizationId && moduleIds.Contains(x.OutputModuleId))
            .ExecuteDeleteAsync();

        // A declared dependency names two Modules and restricts on the one depended upon, for the
        // same reason and with the same consequence for whoever may delete it.
        await dbContext.DependsOnModules
            .Where(x => x.OrganizationId == organizationId && moduleIds.Contains(x.DependsOnModuleId))
            .ExecuteDeleteAsync();

        // A Transfer names two Modules and restricts on both, so it is matched from either side.
        // Open and closed alike: a Module being deleted has nothing left to transfer.
        var transferIds = await dbContext.Transfers
            .Where(t => t.OrganizationId == organizationId &&
                        (moduleIds.Contains(t.ModuleId) || moduleIds.Contains(t.CounterpartyModuleId)))
            .Select(t => t.Id)
            .ToListAsync();

        if (transferIds.Count > 0)
        {
            // The jobs belong to the Module and cascade with it, so they are detached rather than
            // deleted. Their reference to a Transfer is restricted and would block the delete below.
            await dbContext.StateMigrationJobs
                .Where(j => j.OrganizationId == organizationId &&
                            j.TransferId != null && transferIds.Contains(j.TransferId.Value))
                .ExecuteUpdateAsync(u => u.SetProperty(j => j.TransferId, (Guid?)null));

            // Artefacts and locks cascade from the transfer.
            await dbContext.Transfers
                .Where(t => t.OrganizationId == organizationId && transferIds.Contains(t.Id))
                .ExecuteDeleteAsync();
        }

        // A signal edge names two Modules and restricts on both, so it is matched from either side.
        // The edge goes rather than being marked unanswerable: an edge is the relationship to this
        // Module, so an absent edge already says what a status would.
        await dbContext.SignalEdges
            .Where(e => e.OrganizationId == organizationId &&
                        (moduleIds.Contains(e.ConsumerModuleId) || moduleIds.Contains(e.UpstreamModuleId)))
            .ExecuteDeleteAsync();

        // The attempt points at both sets, so it goes before them.
        await dbContext.ModuleProbeAttempts
            .Where(a => a.OrganizationId == organizationId && moduleIds.Contains(a.ModuleId))
            .ExecuteDeleteAsync();

        // A probe's two sets belong to the wave that asked for them and restrict on the Module and
        // on the job that wrote them, so a Module cascading away cannot take them. A wave spans the
        // namespaces of one stack, so a set here can belong to a wave that survives this delete.
        await dbContext.SignalOutputSets
            .Where(x => x.OrganizationId == organizationId && moduleIds.Contains(x.ModuleId))
            .ExecuteDeleteAsync();

        await dbContext.ResourceChangeSets
            .Where(x => x.OrganizationId == organizationId && moduleIds.Contains(x.ModuleId))
            .ExecuteDeleteAsync();
    }
}
