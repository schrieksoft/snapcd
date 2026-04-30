using SnapCd.Contracts;
using SnapCd.Server.Core.Tests.Infrastructure;
using SnapCd.Server.Core.Tests.TestActions;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.UserOrganizationRoleAssignments;

[Collection("NewRoleBasedSharedFixture")]
public class OrganizationOwner_UserOrganizationRoleAssignment_User_Tests : TestBase
{
    public OrganizationOwner_UserOrganizationRoleAssignment_User_Tests(Fixture fixture)
        : base(fixture, CreateConfig(fixture))
    {
    }

    private static TestScenarioConfiguration CreateConfig(Fixture fixture)
    {
        return new TestScenarioConfiguration
        {
            PrincipalId = fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id,
            Discriminator = PrincipalDiscriminator.User,
            TestActionsFactory = (f, db) => new UserOrganizationRoleAssignmentTestActions(f, db),
            NamePrefix = nameof(OrganizationOwner_UserOrganizationRoleAssignment_User_Tests),
            CanGetIds = new[] { fixture.UserOrganizationRoleAssignments["Owner"].Id },
            CanUpdateIds = new[] { fixture.UserOrganizationRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserOrganizationRoleAssignment_User_Tests)}_UpdateCan"].Id },
            CanDeleteIds = new[] { fixture.UserOrganizationRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserOrganizationRoleAssignment_User_Tests)}_DeleteCan"].Id },
            CanCreateParentIds = new[] { Guid.Empty }, // Role assignments don't have a parent
            CannotGetIds = new[] { fixture.UserOrganizationRoleAssignments["Org1Reader"].Id },
            CannotUpdateIds = new[] { fixture.UserOrganizationRoleAssignments["Org1Reader"].Id },
            CannotDeleteIds = new[] { fixture.UserOrganizationRoleAssignments["Org1Reader"].Id },
            CannotCreateParentIds = new[] { Guid.Empty } // Role assignments don't have a parent
        };
    }
}