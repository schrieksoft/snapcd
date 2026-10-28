// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;

namespace SnapCd.Server.Core.Services.PrincipalProvider;

public interface IPrincipalProvider
{
    public Guid GetSubject(Guid organizationId);
    public Guid GetSystemSubject();
    public PrincipalDiscriminator GetPrincipalDiscriminator();
    public List<Guid> GetOrganizations();
    public Guid GetUserId();

    /// <summary>
    /// Gets the subject (principal ID) or returns Guid.Empty if no principal is available.
    /// Use this in contexts where a principal may not be available (e.g., saga activities, background jobs).
    /// </summary>
    public Guid GetSubjectOrDefault(Guid organizationId);

    /// <summary>
    /// Gets the system subject (principal ID) or returns Guid.Empty if no principal is available.
    /// Use this in system contexts where a principal may not be available (e.g., saga activities, background jobs).
    /// </summary>
    public Guid GetSystemSubjectOrDefault();

    /// <summary>
    /// Gets the principal discriminator or returns null if no principal is available.
    /// Use this in contexts where a principal may not be available (e.g., saga activities, background jobs).
    /// Returns null to indicate system/automated operations.
    /// </summary>
    public PrincipalDiscriminator? GetPrincipalDiscriminatorOrDefault();

    /// <summary>
    /// Gets the AgentId from the active <c>agent_id</c> claim, or <c>null</c> if the current
    /// request is not being made by an Agent. The auth identity is always the underlying User or
    /// ServicePrincipal — a non-null AgentId indicates that an Agent is acting via that
    /// ServicePrincipal for this request.
    /// </summary>
    public Guid? GetAgentId();

    /// <summary>
    /// True when an Agent is making the current request (via its underlying ServicePrincipal).
    /// Convenience over <c>GetAgentId() is not null</c> so caller sites don't reimplement the
    /// null-check inconsistently.
    /// </summary>
    public bool IsAgent() => GetAgentId() is not null;
}