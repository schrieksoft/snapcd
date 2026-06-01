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

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.VariableSets;

[Collection("NewRoleBasedSharedFixture")]
public class OrganizationOwner_VariableSet_NestedGroupMember_Tests : TestBase
{
    public OrganizationOwner_VariableSet_NestedGroupMember_Tests(Fixture fixture)
        : base(fixture, CreateConfig(fixture))
    {
    }

    private static TestScenarioConfiguration CreateConfig(Fixture fixture)
    {
        return new TestScenarioConfiguration
        {
            PrincipalId = fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].NestedGroupUser.Id,
            Discriminator = PrincipalDiscriminator.User,
            TestActionsFactory = (f, db) => new VariableSetTestActions(f, db),
            NamePrefix = nameof(OrganizationOwner_VariableSet_NestedGroupMember_Tests),
            CanGetIds = new[] { fixture.VariableSets["00000"].Id, fixture.VariableSets["00001"].Id },
            CannotGetIds = new[] { fixture.VariableSets["10000"].Id },
            // VariableSet is immutable - cannot be updated
            CanUpdateIds = Array.Empty<Guid>(),
            CannotUpdateIds = Array.Empty<Guid>(),
            // VariableSet can only be created by Runner roles, NOT by OrganizationOwner
            CanCreateParentIds = Array.Empty<Guid>(),
            CannotCreateParentIds = Array.Empty<Guid>(),
            // VariableSet can be deleted by OrganizationOwner
            CanDeleteIds = new[] { fixture.VariableSetAdditionalTestEntities[$"{nameof(OrganizationOwner_VariableSet_NestedGroupMember_Tests)}_DeleteCan"].Id },
            CannotDeleteIds = Array.Empty<Guid>()
        };
    }
}