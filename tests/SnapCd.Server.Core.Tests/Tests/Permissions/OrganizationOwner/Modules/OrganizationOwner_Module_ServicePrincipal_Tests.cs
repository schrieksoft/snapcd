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

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.Modules;

/// <summary>
/// Tests for Organization Owner role with Module entity using ServicePrincipal.
/// Organization Owners have full permissions to all modules in the organization.
/// This test class is purely configuration-driven - all test logic is in ScenarioBasedTestBase.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class OrganizationOwner_Module_ServicePrincipal_Tests : TestBase
{
    public OrganizationOwner_Module_ServicePrincipal_Tests(Fixture fixture)
        : base(fixture, CreateConfig(fixture))
    {
    }

    private static TestScenarioConfiguration CreateConfig(Fixture fixture)
    {
        return new TestScenarioConfiguration
        {
            PrincipalId = fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectServicePrincipal.Id,
            Discriminator = PrincipalDiscriminator.ServicePrincipal,
            TestActionsFactory = (f, db) => new ModuleTestActions(f, db),
            NamePrefix = nameof(OrganizationOwner_Module_ServicePrincipal_Tests),

            // Positive cases - Organization Owner should have full access to Org "0"
            CanGetIds = new[] { fixture.Modules["0000"].Id, fixture.Modules["0001"].Id },
            CanUpdateIds = new[] { fixture.ModuleAdditionalTestEntities[$"{nameof(OrganizationOwner_Module_ServicePrincipal_Tests)}_UpdateCan"].Id },
            CanDeleteIds = new[] { fixture.ModuleAdditionalTestEntities[$"{nameof(OrganizationOwner_Module_ServicePrincipal_Tests)}_DeleteCan"].Id },
            CanCreateParentIds = new[] { fixture.Namespaces["000"].Id },

            // Negative cases - Organization Owner should NOT have access to Org "1" (cross-org isolation)
            CannotGetIds = new[] { fixture.Modules["1000"].Id },
            CannotUpdateIds = new[] { fixture.Modules["1000"].Id },
            CannotDeleteIds = new[] { fixture.Modules["1000"].Id },
            CannotCreateParentIds = new[] { fixture.Namespaces["100"].Id }
        };
    }
}