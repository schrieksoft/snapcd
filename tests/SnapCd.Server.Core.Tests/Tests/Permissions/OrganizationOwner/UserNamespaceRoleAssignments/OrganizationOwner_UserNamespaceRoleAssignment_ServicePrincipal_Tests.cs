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

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.UserNamespaceRoleAssignments;

[Collection("NewRoleBasedSharedFixture")]
public class OrganizationOwner_UserNamespaceRoleAssignment_ServicePrincipal_Tests : TestBase
{
    public OrganizationOwner_UserNamespaceRoleAssignment_ServicePrincipal_Tests(Fixture fixture)
        : base(fixture, CreateConfig(fixture))
    {
    }

    private static TestScenarioConfiguration CreateConfig(Fixture fixture)
    {
        return new TestScenarioConfiguration
        {
            PrincipalId = fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectServicePrincipal.Id,
            Discriminator = PrincipalDiscriminator.ServicePrincipal,
            TestActionsFactory = (f, db) => new UserNamespaceRoleAssignmentTestActions(f, db),
            NamePrefix = nameof(OrganizationOwner_UserNamespaceRoleAssignment_ServicePrincipal_Tests),
            CanGetIds = new[] { fixture.UserNamespaceRoleAssignments["Namespace000Reader"].Id },
            CanUpdateIds = new[] { fixture.UserNamespaceRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserNamespaceRoleAssignment_ServicePrincipal_Tests)}_UpdateCan"].Id },
            CanDeleteIds = new[] { fixture.UserNamespaceRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserNamespaceRoleAssignment_ServicePrincipal_Tests)}_DeleteCan"].Id },
            CanCreateParentIds = new[] { fixture.Namespaces["000"].Id },
            CannotGetIds = new[] { fixture.UserNamespaceRoleAssignments["Namespace100Reader"].Id },
            CannotUpdateIds = new[] { fixture.UserNamespaceRoleAssignments["Namespace100Reader"].Id },
            CannotDeleteIds = new[] { fixture.UserNamespaceRoleAssignments["Namespace100Reader"].Id },
            CannotCreateParentIds = new[] { fixture.Namespaces["100"].Id }
        };
    }
}