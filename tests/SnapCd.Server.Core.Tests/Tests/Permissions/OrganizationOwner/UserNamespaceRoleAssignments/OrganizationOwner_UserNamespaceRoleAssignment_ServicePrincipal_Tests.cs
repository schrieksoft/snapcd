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