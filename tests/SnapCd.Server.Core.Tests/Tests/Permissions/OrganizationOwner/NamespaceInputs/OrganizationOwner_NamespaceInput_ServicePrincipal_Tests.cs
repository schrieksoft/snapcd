using SnapCd.Contracts;
using SnapCd.Server.Core.Tests.Infrastructure;
using SnapCd.Server.Core.Tests.TestActions;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.NamespaceInputs;

[Collection("NewRoleBasedSharedFixture")]
public class OrganizationOwner_NamespaceInput_ServicePrincipal_Tests : TestBase
{
    public OrganizationOwner_NamespaceInput_ServicePrincipal_Tests(Fixture fixture)
        : base(fixture, CreateConfig(fixture))
    {
    }

    private static TestScenarioConfiguration CreateConfig(Fixture fixture)
    {
        return new TestScenarioConfiguration
        {
            PrincipalId = fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectServicePrincipal.Id,
            Discriminator = PrincipalDiscriminator.ServicePrincipal,
            TestActionsFactory = (f, db) => new NamespaceInputTestActions(f, db),
            NamePrefix = nameof(OrganizationOwner_NamespaceInput_ServicePrincipal_Tests),
            CanGetIds = new[] { fixture.NamespaceInputs["00000"].Id, fixture.NamespaceInputs["00001"].Id },
            CanUpdateIds = new[] { fixture.NamespaceInputAdditionalTestEntities[$"{nameof(OrganizationOwner_NamespaceInput_ServicePrincipal_Tests)}_UpdateCan"].Id },
            CanDeleteIds = new[] { fixture.NamespaceInputAdditionalTestEntities[$"{nameof(OrganizationOwner_NamespaceInput_ServicePrincipal_Tests)}_DeleteCan"].Id },
            CanCreateParentIds = new[] { fixture.Namespaces["000"].Id },
            CannotGetIds = new[] { fixture.NamespaceInputs["10000"].Id },
            CannotUpdateIds = new[] { fixture.NamespaceInputs["10000"].Id },
            CannotDeleteIds = new[] { fixture.NamespaceInputs["10000"].Id },
            CannotCreateParentIds = new[] { fixture.Namespaces["100"].Id }
        };
    }
}