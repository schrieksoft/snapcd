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
using SnapCd.Server.Core.Entities.Sagas.Base;

namespace SnapCd.Server.Core.Services.Nonsecured.Approvals;

/// <summary>
/// Approvals an apply or a destroy needs, from the Module then its Namespace. Both default to
/// none, so a Module that asks for nothing runs without a gate.
/// </summary>
public static class DeploymentApprovalThreshold
{
    public static async Task<int> Resolve(JobSagaBase saga, Guid moduleId, Guid organizationId, SnapCdDbContext dbContext)
    {
        var thresholds = await dbContext.Modules
            .Include(x => x.Namespace)
            .Where(x => x.Id == moduleId && x.OrganizationId == organizationId)
            .Select(x => new
            {
                Apply = x.ApplyApprovalThreshold ?? x.Namespace.DefaultApplyApprovalThreshold ?? 0,
                Destroy = x.DestroyApprovalThreshold ?? x.Namespace.DefaultDestroyApprovalThreshold ?? 0
            })
            .SingleAsync();

        return saga switch
        {
            ApplyJobSaga => thresholds.Apply,
            DestroyJobSaga => thresholds.Destroy,
            _ => throw new NotImplementedException(
                $"{nameof(DeploymentApprovalThreshold)} not implemented for a saga of type {saga.GetType().Name}")
        };
    }
}
