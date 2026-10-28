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
using SnapCd.Server.Core.Services.Interfaces.PrincipalProvider;

namespace SnapCd.Server.Core.Services.Nonsecured.Admin;

/// <summary>
/// System-level (organization-free) role checks. The Command Center is gated on these.
/// </summary>
public class SystemRoleServiceFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPrincipalProvider principalProvider)
{
    public SystemRoleService Create() => new(dbFactory.CreateDbContext(), principalProvider);
}

public class SystemRoleService : IDisposable
{
    private readonly SnapCdDbContext _dbContext;
    private readonly IPrincipalProvider _principalProvider;

    public SystemRoleService(SnapCdDbContext dbContext, IPrincipalProvider principalProvider)
    {
        _dbContext = dbContext;
        _principalProvider = principalProvider;
    }

    public void Dispose() => _dbContext.Dispose();

    public async Task<bool> UserIsSystemAdministratorAsync(Guid userId)
    {
        if (userId == Guid.Empty) return false;
        return await _dbContext.Set<UserSystemRoleAssignment>()
            .AnyAsync(r => r.UserId == userId && r.RoleName == SystemRole.Administrator);
    }

    public async Task<bool> CurrentUserIsSystemAdministratorAsync()
    {
        if (_principalProvider.GetPrincipalDiscriminatorOrDefault() != PrincipalDiscriminator.User) return false;
        return await UserIsSystemAdministratorAsync(_principalProvider.GetSystemSubjectOrDefault());
    }
}
