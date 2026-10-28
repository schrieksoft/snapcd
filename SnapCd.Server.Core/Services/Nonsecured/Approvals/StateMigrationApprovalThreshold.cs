// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;

namespace SnapCd.Server.Core.Services.Approvals;

/// <summary>
/// Approvals a state migration needs, from the Module then its Namespace. The gate that holds the
/// job and the view that reports it both read this, so a job cannot wait on a number the operator
/// is not shown.
///
/// Unlike apply and destroy, which default to no approvals, a state migration defaults to one: the
/// push is irreversible, so silence is not consent.
/// </summary>
public static class StateMigrationApprovalThreshold
{
    public static async Task<int> Resolve(Guid moduleId, Guid organizationId, SnapCdDbContext dbContext) =>
        await dbContext.Modules
            .Include(x => x.Namespace)
            .Where(x => x.Id == moduleId && x.OrganizationId == organizationId)
            .Select(x => x.StateMigrationApprovalThreshold ?? x.Namespace.DefaultStateMigrationApprovalThreshold ?? 1)
            .SingleAsync();
}
