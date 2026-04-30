using SnapCd.Contracts;
using SnapCd.Server.Core.Tests.Infrastructure;
using SnapCd.Server.Core.Tests.TestActions;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.UserOrganizationRoleAssignments;

[Collection("NewRoleBasedSharedFixture")]
public class OrganizationOwner_UserOrganizationRoleAssignment_ServicePrincipal_Tests : TestBase
{
    public OrganizationOwner_UserOrganizationRoleAssignment_ServicePrincipal_Tests(Fixture fixture)
        : base(fixture, CreateConfig(fixture))
    {
    }

    private static TestScenarioConfiguration CreateConfig(Fixture fixture)
    {
        return new TestScenarioConfiguration
        {
            PrincipalId = fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectServicePrincipal.Id,
            Discriminator = PrincipalDiscriminator.ServicePrincipal,
            TestActionsFactory = (f, db) => new UserOrganizationRoleAssignmentTestActions(f, db),
            NamePrefix = nameof(OrganizationOwner_UserOrganizationRoleAssignment_ServicePrincipal_Tests),
            CanGetIds = new[] { fixture.UserOrganizationRoleAssignments["Owner"].Id },
            CanUpdateIds = new[] { fixture.UserOrganizationRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserOrganizationRoleAssignment_ServicePrincipal_Tests)}_UpdateCan"].Id },
            CanDeleteIds = new[] { fixture.UserOrganizationRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserOrganizationRoleAssignment_ServicePrincipal_Tests)}_DeleteCan"].Id },
            CanCreateParentIds = new[] { Guid.Empty },
            CannotGetIds = new[] { fixture.UserOrganizationRoleAssignments["Org1Reader"].Id },
            CannotUpdateIds = new[] { fixture.UserOrganizationRoleAssignments["Org1Reader"].Id },
            CannotDeleteIds = new[] { fixture.UserOrganizationRoleAssignments["Org1Reader"].Id },
            CannotCreateParentIds = new[] { Guid.Empty }
        };
    }
}