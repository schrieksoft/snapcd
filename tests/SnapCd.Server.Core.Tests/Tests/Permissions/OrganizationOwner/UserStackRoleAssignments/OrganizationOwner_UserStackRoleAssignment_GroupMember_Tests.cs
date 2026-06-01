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

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.UserStackRoleAssignments;

[Collection("NewRoleBasedSharedFixture")]
public class OrganizationOwner_UserStackRoleAssignment_GroupMember_Tests : TestBase
{
    public OrganizationOwner_UserStackRoleAssignment_GroupMember_Tests(Fixture fixture)
        : base(fixture, CreateConfig(fixture))
    {
    }

    private static TestScenarioConfiguration CreateConfig(Fixture fixture)
    {
        return new TestScenarioConfiguration
        {
            PrincipalId = fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].GroupUser.Id,
            Discriminator = PrincipalDiscriminator.User,
            TestActionsFactory = (f, db) => new UserStackRoleAssignmentTestActions(f, db),
            NamePrefix = nameof(OrganizationOwner_UserStackRoleAssignment_GroupMember_Tests),
            CanGetIds = new[] { fixture.UserStackRoleAssignments["Stack00Reader"].Id },
            CanUpdateIds = new[] { fixture.UserStackRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserStackRoleAssignment_GroupMember_Tests)}_UpdateCan"].Id },
            CanDeleteIds = new[] { fixture.UserStackRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserStackRoleAssignment_GroupMember_Tests)}_DeleteCan"].Id },
            CanCreateParentIds = new[] { fixture.Stacks["00"].Id },
            CannotGetIds = new[] { fixture.UserStackRoleAssignments["Stack10Reader"].Id },
            CannotUpdateIds = new[] { fixture.UserStackRoleAssignments["Stack10Reader"].Id },
            CannotDeleteIds = new[] { fixture.UserStackRoleAssignments["Stack10Reader"].Id },
            CannotCreateParentIds = new[] { fixture.Stacks["10"].Id }
        };
    }
}