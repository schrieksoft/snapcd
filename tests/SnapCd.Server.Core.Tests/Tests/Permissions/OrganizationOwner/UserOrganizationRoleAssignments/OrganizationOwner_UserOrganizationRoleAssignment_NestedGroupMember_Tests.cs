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

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.UserOrganizationRoleAssignments;

[Collection("NewRoleBasedSharedFixture")]
public class OrganizationOwner_UserOrganizationRoleAssignment_NestedGroupMember_Tests : TestBase
{
    public OrganizationOwner_UserOrganizationRoleAssignment_NestedGroupMember_Tests(Fixture fixture)
        : base(fixture, CreateConfig(fixture))
    {
    }

    private static TestScenarioConfiguration CreateConfig(Fixture fixture)
    {
        return new TestScenarioConfiguration
        {
            PrincipalId = fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].NestedGroupUser.Id,
            Discriminator = PrincipalDiscriminator.User,
            TestActionsFactory = (f, db) => new UserOrganizationRoleAssignmentTestActions(f, db),
            NamePrefix = nameof(OrganizationOwner_UserOrganizationRoleAssignment_NestedGroupMember_Tests),
            CanGetIds = new[] { fixture.UserOrganizationRoleAssignments["Owner"].Id },
            CanUpdateIds = new[] { fixture.UserOrganizationRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserOrganizationRoleAssignment_NestedGroupMember_Tests)}_UpdateCan"].Id },
            CanDeleteIds = new[] { fixture.UserOrganizationRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserOrganizationRoleAssignment_NestedGroupMember_Tests)}_DeleteCan"].Id },
            CanCreateParentIds = new[] { Guid.Empty },
            CannotGetIds = new[] { fixture.UserOrganizationRoleAssignments["Org1Reader"].Id },
            CannotUpdateIds = new[] { fixture.UserOrganizationRoleAssignments["Org1Reader"].Id },
            CannotDeleteIds = new[] { fixture.UserOrganizationRoleAssignments["Org1Reader"].Id },
            CannotCreateParentIds = new[] { Guid.Empty }
        };
    }
}