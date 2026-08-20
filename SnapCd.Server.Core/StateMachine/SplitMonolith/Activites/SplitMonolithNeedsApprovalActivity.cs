// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.StateMachine.ManualJobs.Activities;

namespace SnapCd.Server.Core.StateMachine.SplitMonolith.Activites;

public class SplitMonolithNeedsApprovalActivity<TMessage>
    : ManualJobNeedsApprovalActivity<SplitMonolithSaga, TMessage>
    where TMessage : class
{
    public SplitMonolithNeedsApprovalActivity(SnapCdDbContext dbContext) : base(dbContext)
    {
    }

    /// Unlike apply and destroy, which default to no approvals, a split defaults to one: the push
    /// is irreversible, so silence is not consent.
    protected override async Task<int> ResolveThreshold(Guid moduleId, Guid organizationId, SnapCdDbContext dbContext) =>
        await dbContext.Modules
            .Include(x => x.Namespace)
            .Where(x => x.Id == moduleId && x.OrganizationId == organizationId)
            .Select(x => x.SplitMonolithApprovalThreshold ?? x.Namespace.DefaultSplitMonolithApprovalThreshold ?? 1)
            .SingleAsync();
}
