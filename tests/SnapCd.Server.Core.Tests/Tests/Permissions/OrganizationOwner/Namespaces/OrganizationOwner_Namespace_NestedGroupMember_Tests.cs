using SnapCd.Contracts;
using SnapCd.Server.Core.Tests.Infrastructure;
using SnapCd.Server.Core.Tests.TestActions;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.Namespaces;

/// <summary>
/// Tests for Organization Owner role with Namespace entity using User as a nested group member.
/// User is in Grandchild → Child → Parent group hierarchy, where Parent has OrgOwner role.
/// Organization Owners have full permissions to all namespaces in the organization.
/// This test class is purely configuration-driven - all test logic is in ScenarioBasedTestBase.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class OrganizationOwner_Namespace_NestedGroupMember_Tests : TestBase
{
    public OrganizationOwner_Namespace_NestedGroupMember_Tests(Fixture fixture)
        : base(fixture, CreateConfig(fixture))
    {
    }

    private static TestScenarioConfiguration CreateConfig(Fixture fixture)
    {
        return new TestScenarioConfiguration
        {
            PrincipalId = fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].NestedGroupUser.Id,
            Discriminator = PrincipalDiscriminator.User,
            TestActionsFactory = (f, db) => new NamespaceTestActions(f, db),
            NamePrefix = nameof(OrganizationOwner_Namespace_NestedGroupMember_Tests),

            // Positive cases - Organization Owner should have full access to Org "0"
            CanGetIds = new[] { fixture.Namespaces["000"].Id, fixture.Namespaces["001"].Id },
            CanUpdateIds = new[] { fixture.NamespaceAdditionalTestEntities[$"{nameof(OrganizationOwner_Namespace_NestedGroupMember_Tests)}_UpdateCan"].Id },
            CanDeleteIds = new[] { fixture.NamespaceAdditionalTestEntities[$"{nameof(OrganizationOwner_Namespace_NestedGroupMember_Tests)}_DeleteCan"].Id },
            CanCreateParentIds = new[] { fixture.Stacks["01"].Id },

            // Negative cases - Organization Owner should NOT have access to Org "1" (cross-org isolation)
            CannotGetIds = new[] { fixture.Namespaces["100"].Id },
            CannotUpdateIds = new[] { fixture.Namespaces["100"].Id },
            CannotDeleteIds = new[] { fixture.Namespaces["100"].Id },
            CannotCreateParentIds = new[] { fixture.Stacks["10"].Id }
        };
    }
}