using SnapCd.Contracts;
using SnapCd.Server.Core.Tests.Infrastructure;
using SnapCd.Server.Core.Tests.TestActions;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.Outputs;

[Collection("NewRoleBasedSharedFixture")]
public class OrganizationOwner_Output_GroupMember_Tests : TestBase
{
    public OrganizationOwner_Output_GroupMember_Tests(Fixture fixture)
        : base(fixture, CreateConfig(fixture))
    {
    }

    private static TestScenarioConfiguration CreateConfig(Fixture fixture)
    {
        return new TestScenarioConfiguration
        {
            PrincipalId = fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].GroupUser.Id,
            Discriminator = PrincipalDiscriminator.User,
            TestActionsFactory = (f, db) => new OutputTestActions(f, db),
            NamePrefix = nameof(OrganizationOwner_Output_GroupMember_Tests),
            CanGetIds = new[] { fixture.Outputs["00000"].Id, fixture.Outputs["00001"].Id },
            CanDeleteIds = new[] { fixture.OutputAdditionalTestEntities[$"{nameof(OrganizationOwner_Output_GroupMember_Tests)}_DeleteCan"].Id },
            CannotGetIds = new[] { fixture.Outputs["10000"].Id },
            CannotDeleteIds = new[] { fixture.Outputs["10000"].Id },
            // Output cannot be updated by anyone, and OrgOwner cannot create Outputs
            CanUpdateIds = Array.Empty<Guid>(),
            CanCreateParentIds = Array.Empty<Guid>(),
            CannotUpdateIds = Array.Empty<Guid>(),
            CannotCreateParentIds = Array.Empty<Guid>()
        };
    }
}