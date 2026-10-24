// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.StateMachine.Gatekeeping.Activities;

public static class ModuleDestroyedCheck
{
    /// <summary>Whether the module's last finished job left it destroyed. Destroying it again achieves nothing.</summary>
    public static async Task<bool> IsDestroyedAsync(SnapCdDbContext dbContext, Guid moduleId, Guid organizationId) =>
        await dbContext.ModuleJobs
            .Where(j => j.ModuleId == moduleId &&
                        j.OrganizationId == organizationId &&
                        j.ActualStateHeadline != null &&
                        j.TimestampEnd != null)
            .OrderByDescending(j => j.JobNumber)
            .Select(j => j.ActualStateHeadline)
            .FirstOrDefaultAsync() == ActualStateHeadline.Destroyed;
}
