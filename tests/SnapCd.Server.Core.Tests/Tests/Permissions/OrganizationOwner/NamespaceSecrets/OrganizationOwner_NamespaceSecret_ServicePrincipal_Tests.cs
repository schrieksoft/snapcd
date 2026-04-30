using SnapCd.Contracts;
using SnapCd.Server.Core.Tests.Infrastructure;
using SnapCd.Server.Core.Tests.TestActions;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.NamespaceSecrets;

[Collection("NewRoleBasedSharedFixture")]
public class OrganizationOwner_NamespaceSecret_ServicePrincipal_Tests : TestBase
{
    public OrganizationOwner_NamespaceSecret_ServicePrincipal_Tests(Fixture fixture)
        : base(fixture, CreateConfig(fixture))
    {
    }

    private static TestScenarioConfiguration CreateConfig(Fixture fixture)
    {
        return new TestScenarioConfiguration
        {
            PrincipalId = fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectServicePrincipal.Id,
            Discriminator = PrincipalDiscriminator.ServicePrincipal,
            TestActionsFactory = (f, db) => new NamespaceSecretTestActions(f, db),
            NamePrefix = nameof(OrganizationOwner_NamespaceSecret_ServicePrincipal_Tests),
            CanGetIds = new[] { fixture.NamespaceSecrets["000"].Id, fixture.NamespaceSecrets["001"].Id },
            CanUpdateIds = new[] { fixture.NamespaceSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_NamespaceSecret_ServicePrincipal_Tests)}_UpdateCan"].Id },
            CanDeleteIds = new[] { fixture.NamespaceSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_NamespaceSecret_ServicePrincipal_Tests)}_DeleteCan"].Id },
            CanCreateParentIds = new[] { fixture.Namespaces["000"].Id },
            CannotGetIds = new[] { fixture.NamespaceSecrets["100"].Id },
            CannotUpdateIds = new[] { fixture.NamespaceSecrets["100"].Id },
            CannotDeleteIds = new[] { fixture.NamespaceSecrets["100"].Id },
            CannotCreateParentIds = new[] { fixture.Namespaces["100"].Id }
        };
    }
}