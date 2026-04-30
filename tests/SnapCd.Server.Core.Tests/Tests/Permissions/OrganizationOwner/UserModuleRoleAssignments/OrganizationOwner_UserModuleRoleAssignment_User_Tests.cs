using SnapCd.Contracts;
using SnapCd.Server.Core.Tests.Infrastructure;
using SnapCd.Server.Core.Tests.TestActions;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.UserModuleRoleAssignments;

[Collection("NewRoleBasedSharedFixture")]
public class OrganizationOwner_UserModuleRoleAssignment_User_Tests : TestBase
{
    public OrganizationOwner_UserModuleRoleAssignment_User_Tests(Fixture fixture)
        : base(fixture, CreateConfig(fixture))
    {
    }

    private static TestScenarioConfiguration CreateConfig(Fixture fixture)
    {
        return new TestScenarioConfiguration
        {
            PrincipalId = fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id,
            Discriminator = PrincipalDiscriminator.User,
            TestActionsFactory = (f, db) => new UserModuleRoleAssignmentTestActions(f, db),
            NamePrefix = nameof(OrganizationOwner_UserModuleRoleAssignment_User_Tests),
            CanGetIds = new[] { fixture.UserModuleRoleAssignments["Module0000Reader"].Id },
            CanUpdateIds = new[] { fixture.UserModuleRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserModuleRoleAssignment_User_Tests)}_UpdateCan"].Id },
            CanDeleteIds = new[] { fixture.UserModuleRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserModuleRoleAssignment_User_Tests)}_DeleteCan"].Id },
            CanCreateParentIds = new[] { fixture.Modules["0000"].Id },
            CannotGetIds = new[] { fixture.UserModuleRoleAssignments["Module1000Reader"].Id },
            CannotUpdateIds = new[] { fixture.UserModuleRoleAssignments["Module1000Reader"].Id },
            CannotDeleteIds = new[] { fixture.UserModuleRoleAssignments["Module1000Reader"].Id },
            CannotCreateParentIds = new[] { fixture.Modules["1000"].Id }
        };
    }
}