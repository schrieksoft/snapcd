using SnapCd.Contracts;
using SnapCd.Server.Core.Tests.Infrastructure;
using SnapCd.Server.Core.Tests.TestActions;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.ModuleSecrets;

[Collection("NewRoleBasedSharedFixture")]
public class OrganizationOwner_ModuleSecret_NestedGroupMember_Tests : TestBase
{
    public OrganizationOwner_ModuleSecret_NestedGroupMember_Tests(Fixture fixture)
        : base(fixture, CreateConfig(fixture))
    {
    }

    private static TestScenarioConfiguration CreateConfig(Fixture fixture)
    {
        return new TestScenarioConfiguration
        {
            PrincipalId = fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].NestedGroupUser.Id,
            Discriminator = PrincipalDiscriminator.User,
            TestActionsFactory = (f, db) => new ModuleSecretTestActions(f, db),
            NamePrefix = nameof(OrganizationOwner_ModuleSecret_NestedGroupMember_Tests),
            CanGetIds = new[] { fixture.ModuleSecrets["0000"].Id, fixture.ModuleSecrets["0001"].Id },
            CanUpdateIds = new[] { fixture.ModuleSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_ModuleSecret_NestedGroupMember_Tests)}_UpdateCan"].Id },
            CanDeleteIds = new[] { fixture.ModuleSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_ModuleSecret_NestedGroupMember_Tests)}_DeleteCan"].Id },
            CanCreateParentIds = new[] { fixture.Modules["0000"].Id },
            CannotGetIds = new[] { fixture.ModuleSecrets["1000"].Id },
            CannotUpdateIds = new[] { fixture.ModuleSecrets["1000"].Id },
            CannotDeleteIds = new[] { fixture.ModuleSecrets["1000"].Id },
            CannotCreateParentIds = new[] { fixture.Modules["1000"].Id }
        };
    }
}