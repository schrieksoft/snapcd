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
        // The Namespace value comes from the resolved-defaults view, so a threshold set on any
        // Namespace above the Module's own applies to it.
        var thresholds = await (
            from x in dbContext.Modules
            join d in dbContext.NamespaceResolvedDefaults
                on new { NamespaceId = x.NamespaceId, x.OrganizationId } equals new { d.NamespaceId, d.OrganizationId }
                into resolved
            from d in resolved.DefaultIfEmpty()
            where x.Id == moduleId && x.OrganizationId == organizationId
            select new
            {
                Apply = x.ApplyApprovalThreshold ?? (d == null ? null : d.ApplyApprovalThreshold) ?? 0,
                Destroy = x.DestroyApprovalThreshold ?? (d == null ? null : d.DestroyApprovalThreshold) ?? 0
            }).SingleAsync();

        return saga switch
        {
            ApplyJobSaga => thresholds.Apply,
            DestroyJobSaga => thresholds.Destroy,
            _ => throw new NotImplementedException(
                $"{nameof(DeploymentApprovalThreshold)} not implemented for a saga of type {saga.GetType().Name}")
        };
    }
}
