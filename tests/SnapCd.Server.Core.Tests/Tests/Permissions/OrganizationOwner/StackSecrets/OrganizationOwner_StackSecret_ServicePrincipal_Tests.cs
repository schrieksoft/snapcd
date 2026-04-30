using SnapCd.Contracts;
using SnapCd.Server.Core.Tests.Infrastructure;
using SnapCd.Server.Core.Tests.TestActions;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.StackSecrets;

[Collection("NewRoleBasedSharedFixture")]
public class OrganizationOwner_StackSecret_ServicePrincipal_Tests : TestBase
{
    public OrganizationOwner_StackSecret_ServicePrincipal_Tests(Fixture fixture)
        : base(fixture, CreateConfig(fixture))
    {
    }

    private static TestScenarioConfiguration CreateConfig(Fixture fixture)
    {
        return new TestScenarioConfiguration
        {
            PrincipalId = fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectServicePrincipal.Id,
            Discriminator = PrincipalDiscriminator.ServicePrincipal,
            TestActionsFactory = (f, db) => new StackSecretTestActions(f, db),
            NamePrefix = nameof(OrganizationOwner_StackSecret_ServicePrincipal_Tests),
            CanGetIds = new[] { fixture.StackSecrets["000"].Id, fixture.StackSecrets["001"].Id },
            CanUpdateIds = new[] { fixture.StackSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_StackSecret_ServicePrincipal_Tests)}_UpdateCan"].Id },
            CanDeleteIds = new[] { fixture.StackSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_StackSecret_ServicePrincipal_Tests)}_DeleteCan"].Id },
            CanCreateParentIds = new[] { fixture.Stacks["00"].Id },
            CannotGetIds = new[] { fixture.StackSecrets["100"].Id },
            CannotUpdateIds = new[] { fixture.StackSecrets["100"].Id },
            CannotDeleteIds = new[] { fixture.StackSecrets["100"].Id },
            CannotCreateParentIds = new[] { fixture.Stacks["10"].Id }
        };
    }
}