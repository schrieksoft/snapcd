// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.Server.Core.Tests.Infrastructure;

public class TestPrincipalProvider : IPrincipalProvider
{
    private readonly Guid _principalId;
    private readonly PrincipalDiscriminator _discriminator;
    private readonly Guid _organizationId;
    private readonly Guid? _agentId;

    public TestPrincipalProvider(Guid principalId, PrincipalDiscriminator discriminator, Guid organizationId, Guid? agentId = null)
    {
        _principalId = principalId;
        _discriminator = discriminator;
        _organizationId = organizationId;
        _agentId = agentId;
    }

    public Guid GetSystemSubject()
    {
        return _principalId;
    }

    public Guid GetSubject(Guid organizationId)
    {
        return _principalId;
    }

    public PrincipalDiscriminator GetPrincipalDiscriminator()
    {
        return _discriminator;
    }

    public List<Guid> GetOrganizations()
    {
        return new List<Guid> { _organizationId };
    }

    public Guid GetUserId()
    {
        // For service principals, this would throw or return Guid.Empty
        // For users, this returns the user ID
        if (_discriminator == PrincipalDiscriminator.User) return _principalId;
        return Guid.Empty;
    }

    public Guid GetSubjectOrDefault(Guid organizationId)
    {
        return _principalId;
    }

    public Guid GetSystemSubjectOrDefault()
    {
        return _principalId;
    }

    public PrincipalDiscriminator? GetPrincipalDiscriminatorOrDefault()
    {
        return _discriminator;
    }

    public Guid? GetAgentId()
    {
        return _agentId;
    }
}