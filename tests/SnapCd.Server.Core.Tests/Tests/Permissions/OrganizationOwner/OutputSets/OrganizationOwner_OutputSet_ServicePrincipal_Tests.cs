using SnapCd.Contracts;
using SnapCd.Server.Core.Tests.Infrastructure;
using SnapCd.Server.Core.Tests.TestActions;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.OutputSets;

[Collection("NewRoleBasedSharedFixture")]
public class OrganizationOwner_OutputSet_ServicePrincipal_Tests : TestBase
{
    public OrganizationOwner_OutputSet_ServicePrincipal_Tests(Fixture fixture)
        : base(fixture, CreateConfig(fixture))
    {
    }

    private static TestScenarioConfiguration CreateConfig(Fixture fixture)
    {
        return new TestScenarioConfiguration
        {
            PrincipalId = fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectServicePrincipal.Id,
            Discriminator = PrincipalDiscriminator.ServicePrincipal,
            TestActionsFactory = (f, db) => new OutputSetTestActions(f, db),
            NamePrefix = nameof(OrganizationOwner_OutputSet_ServicePrincipal_Tests),
            CanGetIds = new[] { fixture.OutputSets["00000"].Id, fixture.OutputSets["00001"].Id },
            CanDeleteIds = new[] { fixture.OutputSetAdditionalTestEntities[$"{nameof(OrganizationOwner_OutputSet_ServicePrincipal_Tests)}_DeleteCan"].Id },
            CannotGetIds = new[] { fixture.OutputSets["10000"].Id },
            CannotDeleteIds = new[] { fixture.OutputSets["10000"].Id },
            // OutputSet cannot be updated by anyone, and OrgOwner cannot create OutputSets
            CanUpdateIds = Array.Empty<Guid>(),
            CanCreateParentIds = Array.Empty<Guid>(),
            CannotUpdateIds = Array.Empty<Guid>(),
            CannotCreateParentIds = Array.Empty<Guid>()
        };
    }
}