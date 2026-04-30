using SnapCd.Contracts;
using SnapCd.Server.Core.Tests.Infrastructure;
using SnapCd.Server.Core.Tests.TestActions;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.UserStackRoleAssignments;

[Collection("NewRoleBasedSharedFixture")]
public class OrganizationOwner_UserStackRoleAssignment_User_Tests : TestBase
{
    public OrganizationOwner_UserStackRoleAssignment_User_Tests(Fixture fixture)
        : base(fixture, CreateConfig(fixture))
    {
    }

    private static TestScenarioConfiguration CreateConfig(Fixture fixture)
    {
        return new TestScenarioConfiguration
        {
            PrincipalId = fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id,
            Discriminator = PrincipalDiscriminator.User,
            TestActionsFactory = (f, db) => new UserStackRoleAssignmentTestActions(f, db),
            NamePrefix = nameof(OrganizationOwner_UserStackRoleAssignment_User_Tests),
            CanGetIds = new[] { fixture.UserStackRoleAssignments["Stack00Reader"].Id },
            CanUpdateIds = new[] { fixture.UserStackRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserStackRoleAssignment_User_Tests)}_UpdateCan"].Id },
            CanDeleteIds = new[] { fixture.UserStackRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserStackRoleAssignment_User_Tests)}_DeleteCan"].Id },
            CanCreateParentIds = new[] { fixture.Stacks["00"].Id },
            CannotGetIds = new[] { fixture.UserStackRoleAssignments["Stack10Reader"].Id },
            CannotUpdateIds = new[] { fixture.UserStackRoleAssignments["Stack10Reader"].Id },
            CannotDeleteIds = new[] { fixture.UserStackRoleAssignments["Stack10Reader"].Id },
            CannotCreateParentIds = new[] { fixture.Stacks["10"].Id }
        };
    }
}