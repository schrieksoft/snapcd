// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Custom.Nonsecured;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.Server.Core.Repositories.Custom.Secured;

public class UserColorSecuredRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory)
{
    public UserColorSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var repositoryFactory = new UserColorRepositoryFactory(dbFactory);
        return new UserColorSecuredRepository(repositoryFactory.Create(), principalProvider);
    }
}

/// <summary>
/// Colours are strictly personal: every operation is scoped to the current principal's user id.
/// Only users have colours — service principals get empty reads and are not allowed to write.
///
/// No permission check is made against the target itself. A colour carries no information about
/// the target beyond an id the caller already had, and reads are filtered to the caller's own
/// rows, so there is nothing to leak. Colours pointing at deleted or no-longer-visible targets
/// are simply never matched when rendering.
/// </summary>
public class UserColorSecuredRepository : IDisposable
{
    private static readonly Regex HexColor = new("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$",
        RegexOptions.Compiled);

    private readonly UserColorRepository _repository;
    private readonly IPrincipalProvider _principalProvider;

    public UserColorSecuredRepository(UserColorRepository repository, IPrincipalProvider principalProvider)
    {
        _repository = repository;
        _principalProvider = principalProvider;
    }

    private bool IsUser => _principalProvider.GetPrincipalDiscriminator() == PrincipalDiscriminator.User;

    public async Task<List<UserColor>> List(Guid organizationId)
    {
        if (!IsUser) return new List<UserColor>();
        return await _repository.ListByUser(_principalProvider.GetSubject(organizationId), organizationId);
    }

    public async Task<UserColor?> GetByTarget(Guid organizationId, ColorTargetType targetType, Guid targetId)
    {
        if (!IsUser) return null;
        return await _repository.GetByTarget(_principalProvider.GetSubject(organizationId), organizationId, targetType,
            targetId);
    }

    /// <summary>
    /// Sets the colour on a target, replacing any existing one. Passing a null or empty colour
    /// clears it, so the UI's "no colour" option is the same call as setting one.
    /// </summary>
    public async Task<UserColor?> Set(Guid organizationId, ColorTargetType targetType, Guid targetId, string? color)
    {
        if (!IsUser)
            throw new PrincipalNotAuthorizedException("Only users can set colors.");

        var userId = _principalProvider.GetSubject(organizationId);

        if (string.IsNullOrWhiteSpace(color))
        {
            await _repository.DeleteByTarget(userId, organizationId, targetType, targetId);
            return null;
        }

        color = color.Trim();
        if (!HexColor.IsMatch(color))
            throw new ArgumentException($"'{color}' is not a valid hex colour (expected e.g. \"#E85D1A\").");

        var now = DateTime.UtcNow;
        return await _repository.Upsert(new UserColor
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = userId,
            TargetType = targetType,
            TargetId = targetId,
            Color = color,
            CreatedBy = userId,
            CreatedByPrincipalDiscriminator = AuditPrincipalDiscriminator.User,
            CreatedDateTime = now,
            ModifiedBy = userId,
            ModifiedByPrincipalDiscriminator = AuditPrincipalDiscriminator.User,
            ModifiedDateTime = now
        });
    }

    public async Task Delete(Guid organizationId, ColorTargetType targetType, Guid targetId)
    {
        if (!IsUser)
            throw new PrincipalNotAuthorizedException("Only users can delete colors.");

        // Nonsecured delete is filtered by user id, so users can only ever delete their own colours
        await _repository.DeleteByTarget(_principalProvider.GetSubject(organizationId), organizationId, targetType,
            targetId);
    }

    public void Dispose()
    {
        _repository.Dispose();
    }
}
