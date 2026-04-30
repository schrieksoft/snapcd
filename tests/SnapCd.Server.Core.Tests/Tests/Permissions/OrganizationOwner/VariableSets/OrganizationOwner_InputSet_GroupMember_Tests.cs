using SnapCd.Contracts;
using SnapCd.Server.Core.Tests.Infrastructure;
using SnapCd.Server.Core.Tests.TestActions;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.VariableSets;

[Collection("NewRoleBasedSharedFixture")]
public class OrganizationOwner_VariableSet_GroupMember_Tests : TestBase
{
    public OrganizationOwner_VariableSet_GroupMember_Tests(Fixture fixture)
        : base(fixture, CreateConfig(fixture))
    {
    }

    private static TestScenarioConfiguration CreateConfig(Fixture fixture)
    {
        return new TestScenarioConfiguration
        {
            PrincipalId = fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].GroupUser.Id,
            Discriminator = PrincipalDiscriminator.User,
            TestActionsFactory = (f, db) => new VariableSetTestActions(f, db),
            NamePrefix = nameof(OrganizationOwner_VariableSet_GroupMember_Tests),
            CanGetIds = new[] { fixture.VariableSets["00000"].Id, fixture.VariableSets["00001"].Id },
            CannotGetIds = new[] { fixture.VariableSets["10000"].Id },
            // VariableSet is immutable - cannot be updated
            CanUpdateIds = Array.Empty<Guid>(),
            CannotUpdateIds = Array.Empty<Guid>(),
            // VariableSet can only be created by Runner roles, NOT by OrganizationOwner
            CanCreateParentIds = Array.Empty<Guid>(),
            CannotCreateParentIds = Array.Empty<Guid>(),
            // VariableSet can be deleted by OrganizationOwner
            CanDeleteIds = new[] { fixture.VariableSetAdditionalTestEntities[$"{nameof(OrganizationOwner_VariableSet_GroupMember_Tests)}_DeleteCan"].Id },
            CannotDeleteIds = Array.Empty<Guid>()
        };
    }
}