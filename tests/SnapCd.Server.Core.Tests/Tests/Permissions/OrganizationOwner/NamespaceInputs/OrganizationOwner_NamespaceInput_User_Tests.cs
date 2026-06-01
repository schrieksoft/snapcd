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

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.NamespaceInputs;

/// <summary>
/// Tests for Organization Owner role with NamespaceInput entity using User principal (direct assignment only).
/// Organization Owners have full permissions to all namespace inputs in the organization.
/// This test class is purely configuration-driven - all test logic is in ScenarioBasedTestBase.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class OrganizationOwner_NamespaceInput_User_Tests : TestBase
{
    public OrganizationOwner_NamespaceInput_User_Tests(Fixture fixture)
        : base(fixture, CreateConfig(fixture))
    {
    }

    private static TestScenarioConfiguration CreateConfig(Fixture fixture)
    {
        return new TestScenarioConfiguration
        {
            PrincipalId = fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id,
            Discriminator = PrincipalDiscriminator.User,
            TestActionsFactory = (f, db) => new NamespaceInputTestActions(f, db),
            NamePrefix = nameof(OrganizationOwner_NamespaceInput_User_Tests),

            // Positive cases - Organization Owner should have full access to Org "0"
            CanGetIds = new[] { fixture.NamespaceInputs["00000"].Id, fixture.NamespaceInputs["00001"].Id },
            CanUpdateIds = new[] { fixture.NamespaceInputAdditionalTestEntities[$"{nameof(OrganizationOwner_NamespaceInput_User_Tests)}_UpdateCan"].Id },
            CanDeleteIds = new[] { fixture.NamespaceInputAdditionalTestEntities[$"{nameof(OrganizationOwner_NamespaceInput_User_Tests)}_DeleteCan"].Id },
            CanCreateParentIds = new[] { fixture.Namespaces["000"].Id },

            // Negative cases - Organization Owner should NOT have access to Org "1" (cross-org isolation)
            CannotGetIds = new[] { fixture.NamespaceInputs["10000"].Id },
            CannotUpdateIds = new[] { fixture.NamespaceInputs["10000"].Id },
            CannotDeleteIds = new[] { fixture.NamespaceInputs["10000"].Id },
            CannotCreateParentIds = new[] { fixture.Namespaces["100"].Id }
        };
    }
}