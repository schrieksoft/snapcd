// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.System;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.Server.Core.Services.Admin;

/// <summary>
/// System-level (organization-free) role checks. The Command Center is gated on these.
/// </summary>
public class SystemRoleService
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly IPrincipalProvider _principalProvider;

    public SystemRoleService(IDbContextFactory<SnapCdDbContext> dbContextFactory, IPrincipalProvider principalProvider)
    {
        _dbContextFactory = dbContextFactory;
        _principalProvider = principalProvider;
    }

    public async Task<bool> UserIsSystemAdministratorAsync(Guid userId)
    {
        if (userId == Guid.Empty) return false;
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.Set<UserSystemRoleAssignment>()
            .AnyAsync(r => r.UserId == userId && r.RoleName == SystemRole.Administrator);
    }

    public async Task<bool> CurrentUserIsSystemAdministratorAsync()
    {
        if (_principalProvider.GetPrincipalDiscriminatorOrDefault() != PrincipalDiscriminator.User) return false;
        return await UserIsSystemAdministratorAsync(_principalProvider.GetSystemSubjectOrDefault());
    }
}
