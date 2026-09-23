// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Services.Approvals;
using SnapCd.Server.Core.StateMachine.ManualJobs.Activities;

namespace SnapCd.Server.Core.StateMachine.Transfers.Migrate.Activities;

/// <summary>
/// A transfer's write is approved against the same threshold as any other state migration: the one
/// on the Module whose state is about to change.
/// </summary>
public class TransferMigrateNeedsApprovalActivity<TMessage>
    : ManualJobNeedsApprovalActivity<TransferMigrateSaga, TMessage>
    where TMessage : class
{
    public TransferMigrateNeedsApprovalActivity(SnapCdDbContext dbContext) : base(dbContext)
    {
    }

    protected override Task<int> ResolveThreshold(Guid moduleId, Guid organizationId, SnapCdDbContext dbContext) =>
        StateMigrationApprovalThreshold.Resolve(moduleId, organizationId, dbContext);
}
