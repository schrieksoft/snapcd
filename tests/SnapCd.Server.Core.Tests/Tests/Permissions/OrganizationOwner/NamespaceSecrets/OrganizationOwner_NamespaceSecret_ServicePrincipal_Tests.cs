// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Server.Core.Tests.Infrastructure;
using SnapCd.Server.Core.Tests.TestActions;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.NamespaceSecrets;

[Collection("NewRoleBasedSharedFixture")]
public class OrganizationOwner_NamespaceSecret_ServicePrincipal_Tests : TestBase
{
    public OrganizationOwner_NamespaceSecret_ServicePrincipal_Tests(Fixture fixture)
        : base(fixture, CreateConfig(fixture))
    {
    }

    private static TestScenarioConfiguration CreateConfig(Fixture fixture)
    {
        return new TestScenarioConfiguration
        {
            PrincipalId = fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectServicePrincipal.Id,
            Discriminator = PrincipalDiscriminator.ServicePrincipal,
            TestActionsFactory = (f, db) => new NamespaceSecretTestActions(f, db),
            NamePrefix = nameof(OrganizationOwner_NamespaceSecret_ServicePrincipal_Tests),
            CanGetIds = new[] { fixture.NamespaceSecrets["000"].Id, fixture.NamespaceSecrets["001"].Id },
            CanUpdateIds = new[] { fixture.NamespaceSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_NamespaceSecret_ServicePrincipal_Tests)}_UpdateCan"].Id },
            CanDeleteIds = new[] { fixture.NamespaceSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_NamespaceSecret_ServicePrincipal_Tests)}_DeleteCan"].Id },
            CanCreateParentIds = new[] { fixture.Namespaces["000"].Id },
            CannotGetIds = new[] { fixture.NamespaceSecrets["100"].Id },
            CannotUpdateIds = new[] { fixture.NamespaceSecrets["100"].Id },
            CannotDeleteIds = new[] { fixture.NamespaceSecrets["100"].Id },
            CannotCreateParentIds = new[] { fixture.Namespaces["100"].Id }
        };
    }
}