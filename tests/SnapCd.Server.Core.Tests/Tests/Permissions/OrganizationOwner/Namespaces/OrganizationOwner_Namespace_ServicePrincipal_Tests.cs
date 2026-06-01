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

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.Namespaces;

/// <summary>
/// Tests for Organization Owner role with Namespace entity using ServicePrincipal.
/// Organization Owners have full permissions to all namespaces in the organization.
/// This test class is purely configuration-driven - all test logic is in ScenarioBasedTestBase.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class OrganizationOwner_Namespace_ServicePrincipal_Tests : TestBase
{
    public OrganizationOwner_Namespace_ServicePrincipal_Tests(Fixture fixture)
        : base(fixture, CreateConfig(fixture))
    {
    }

    private static TestScenarioConfiguration CreateConfig(Fixture fixture)
    {
        return new TestScenarioConfiguration
        {
            PrincipalId = fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectServicePrincipal.Id,
            Discriminator = PrincipalDiscriminator.ServicePrincipal,
            TestActionsFactory = (f, db) => new NamespaceTestActions(f, db),
            NamePrefix = nameof(OrganizationOwner_Namespace_ServicePrincipal_Tests),

            // Positive cases - Organization Owner should have full access to Org "0"
            CanGetIds = new[] { fixture.Namespaces["000"].Id, fixture.Namespaces["001"].Id },
            CanUpdateIds = new[] { fixture.NamespaceAdditionalTestEntities[$"{nameof(OrganizationOwner_Namespace_ServicePrincipal_Tests)}_UpdateCan"].Id },
            CanDeleteIds = new[] { fixture.NamespaceAdditionalTestEntities[$"{nameof(OrganizationOwner_Namespace_ServicePrincipal_Tests)}_DeleteCan"].Id },
            CanCreateParentIds = new[] { fixture.Stacks["00"].Id },

            // Negative cases - Organization Owner should NOT have access to Org "1" (cross-org isolation)
            CannotGetIds = new[] { fixture.Namespaces["100"].Id },
            CannotUpdateIds = new[] { fixture.Namespaces["100"].Id },
            CannotDeleteIds = new[] { fixture.Namespaces["100"].Id },
            CannotCreateParentIds = new[] { fixture.Stacks["10"].Id }
        };
    }
}