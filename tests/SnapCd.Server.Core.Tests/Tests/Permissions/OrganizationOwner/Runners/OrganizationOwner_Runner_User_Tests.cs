using SnapCd.Contracts;
using SnapCd.Server.Core.Tests.Infrastructure;
using SnapCd.Server.Core.Tests.TestActions;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.Runners;

/// <summary>
/// Tests for Organization Owner role with Runner entity using User principal (direct assignment only).
/// Organization Owners have full permissions to all Runners in the organization.
/// This test class is purely configuration-driven - all test logic is in ScenarioBasedTestBase.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class OrganizationOwner_Runner_User_Tests : TestBase
{
    public OrganizationOwner_Runner_User_Tests(Fixture fixture)
        : base(fixture, CreateConfig(fixture))
    {
    }

    private static TestScenarioConfiguration CreateConfig(Fixture fixture)
    {
        return new TestScenarioConfiguration
        {
            PrincipalId = fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id,
            Discriminator = PrincipalDiscriminator.User,
            TestActionsFactory = (f, db) => new RunnerTestActions(f, db),
            NamePrefix = nameof(OrganizationOwner_Runner_User_Tests),

            // Positive cases - Organization Owner should have full access to Org "0"
            CanGetIds = new[] { fixture.Runners["0"].Id },
            CanUpdateIds = new[] { fixture.RunnerAdditionalTestEntities[$"{nameof(OrganizationOwner_Runner_User_Tests)}_UpdateCan"].Id },
            CanDeleteIds = new[] { fixture.RunnerAdditionalTestEntities[$"{nameof(OrganizationOwner_Runner_User_Tests)}_DeleteCan"].Id },
            CanCreateParentIds = new[] { fixture.Organizations["0"].Id },

            // Negative cases - Organization Owner should NOT have access to Org "1" (cross-org isolation)
            CannotGetIds = new[] { fixture.Runners["1"].Id },
            CannotUpdateIds = new[] { fixture.Runners["1"].Id },
            CannotDeleteIds = new[] { fixture.Runners["1"].Id },
            CannotCreateParentIds = new[] { fixture.Organizations["1"].Id }
        };
    }
}