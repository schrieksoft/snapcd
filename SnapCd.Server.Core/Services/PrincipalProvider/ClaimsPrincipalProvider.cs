// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Security.Claims;
using SnapCd.Contracts;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Misc.Exceptions;

namespace SnapCd.Server.Core.Services.PrincipalProvider;

/// <summary>
/// <see cref="IPrincipalProvider"/> backed by an explicit <see cref="ClaimsPrincipal"/> rather than
/// <see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor"/>. Use this inside a SignalR hub: the
/// ambient HTTP accessor is unreliable there (typically null for the WebSocket transport — see
/// learn.microsoft.com/aspnet/core/signalr/httpcontext), but the connection's authenticated principal
/// is available on <c>Hub.Context.User</c>. Pass that in so the repository layer can attribute audit
/// fields (incl. <c>CreatedByAgentId</c>) to the connecting agent instead of falling through to System.
/// </summary>
public class ClaimsPrincipalProvider : IPrincipalProvider
{
    private readonly ClaimsPrincipal? _user;

    public ClaimsPrincipalProvider(ClaimsPrincipal? user)
    {
        _user = user;
    }

    private string? Claim(string type) => _user?.FindFirst(type)?.Value;

    public Guid GetSystemSubject()
        => ParseGuid(Claim(ClaimTypeConstants.SubjectClaimType))
           ?? throw new PrincipalNotAuthorizedException("No principal found");

    public Guid GetSubject(Guid organizationId)
    {
        var subject = ParseGuid(Claim(ClaimTypeConstants.SubjectClaimType));
        if (subject is null)
            throw new PrincipalNotAuthorizedException("No principal found");
        if (!GetOrganizations().Contains(organizationId))
            throw new PrincipalNotAuthorizedException($"Principal not member of organization {organizationId}");
        return subject.Value;
    }

    public PrincipalDiscriminator GetPrincipalDiscriminator()
        => GetPrincipalDiscriminatorOrDefault() ?? PrincipalDiscriminator.User;

    public List<Guid> GetOrganizations()
    {
        var claim = Claim(ClaimTypeConstants.OrganizationClaimType);
        if (string.IsNullOrEmpty(claim))
            return new List<Guid>();
        return claim.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(o => new Guid(o.Trim()))
            .ToList();
    }

    public Guid GetUserId()
    {
        if (GetPrincipalDiscriminator() != PrincipalDiscriminator.User)
            throw new PrincipalNotAuthorizedException("GetUserId() can only be called for User principals");
        return ParseGuid(Claim(ClaimTypeConstants.SubjectClaimType))
               ?? throw new PrincipalNotAuthorizedException("No user principal found");
    }

    // The connection's org membership is already validated at OnConnectedAsync, so the subject is
    // returned directly here (no DB re-check); Guid.Empty when there is no principal at all.
    public Guid GetSubjectOrDefault(Guid organizationId)
        => ParseGuid(Claim(ClaimTypeConstants.SubjectClaimType)) ?? Guid.Empty;

    public Guid GetSystemSubjectOrDefault()
        => ParseGuid(Claim(ClaimTypeConstants.SubjectClaimType)) ?? Guid.Empty;

    public PrincipalDiscriminator? GetPrincipalDiscriminatorOrDefault()
        => Claim(ClaimTypeConstants.PrincipalDiscriminatorClaimType) switch
        {
            "ServicePrincipal" => PrincipalDiscriminator.ServicePrincipal,
            "User" => PrincipalDiscriminator.User,
            _ => null
        };

    public Guid? GetAgentId() => ParseGuid(Claim(ClaimTypeConstants.AgentClaimType));

    private static Guid? ParseGuid(string? value) => Guid.TryParse(value, out var g) ? g : null;
}
