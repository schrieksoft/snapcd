using SnapCd.Contracts;
using SnapCd.Server.Core.Tests.Infrastructure;
using SnapCd.Server.Core.Tests.TestActions;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.LiteralOutputs;

[Collection("NewRoleBasedSharedFixture")]
public class OrganizationOwner_LiteralOutput_ServicePrincipal_Tests : TestBase
{
    public OrganizationOwner_LiteralOutput_ServicePrincipal_Tests(Fixture fixture)
        : base(fixture, CreateConfig(fixture))
    {
    }

    private static TestScenarioConfiguration CreateConfig(Fixture fixture)
    {
        return new TestScenarioConfiguration
        {
            PrincipalId = fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectServicePrincipal.Id,
            Discriminator = PrincipalDiscriminator.ServicePrincipal,
            TestActionsFactory = (f, db) => new LiteralOutputTestActions(f, db),
            NamePrefix = nameof(OrganizationOwner_LiteralOutput_ServicePrincipal_Tests),
            CanGetIds = new[] { fixture.Outputs["00000"].Id, fixture.Outputs["00001"].Id },
            CannotGetIds = new[] { fixture.Outputs["10000"].Id },
            // LiteralOutput is read-only - cannot be created, updated, or deleted
            CanUpdateIds = Array.Empty<Guid>(),
            CanDeleteIds = Array.Empty<Guid>(),
            CanCreateParentIds = Array.Empty<Guid>(),
            CannotUpdateIds = Array.Empty<Guid>(),
            CannotDeleteIds = Array.Empty<Guid>(),
            CannotCreateParentIds = Array.Empty<Guid>()
        };
    }
}