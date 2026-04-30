using SnapCd.Contracts;
using SnapCd.Server.Core.Tests.Infrastructure;
using SnapCd.Server.Core.Tests.TestActions;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.ModuleInput;

/// <summary>
/// Tests for Organization Owner role with ModuleInput entity using ServicePrincipal.
/// Organization Owners have full permissions to all module inputs in the organization.
/// This test class is purely configuration-driven - all test logic is in ScenarioBasedTestBase.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class OrganizationOwner_ModuleInput_ServicePrincipal_Tests : TestBase
{
    public OrganizationOwner_ModuleInput_ServicePrincipal_Tests(Fixture fixture)
        : base(fixture, CreateConfig(fixture))
    {
    }

    private static TestScenarioConfiguration CreateConfig(Fixture fixture)
    {
        return new TestScenarioConfiguration
        {
            PrincipalId = fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectServicePrincipal.Id,
            Discriminator = PrincipalDiscriminator.ServicePrincipal,
            TestActionsFactory = (f, db) => new ModuleInputTestActions(f, db),
            NamePrefix = nameof(OrganizationOwner_ModuleInput_ServicePrincipal_Tests),

            // Positive cases - Organization Owner should have full access to Org "0"
            CanGetIds = new[] { fixture.ModuleInputs["00000"].Id, fixture.ModuleInputs["00001"].Id },
            CanUpdateIds = new[] { fixture.ModuleInputAdditionalTestEntities[$"{nameof(OrganizationOwner_ModuleInput_ServicePrincipal_Tests)}_UpdateCan"].Id },
            CanDeleteIds = new[] { fixture.ModuleInputAdditionalTestEntities[$"{nameof(OrganizationOwner_ModuleInput_ServicePrincipal_Tests)}_DeleteCan"].Id },
            CanCreateParentIds = new[] { fixture.Modules["0000"].Id },

            // Negative cases - Organization Owner should NOT have access to Org "1" (cross-org isolation)
            CannotGetIds = new[] { fixture.ModuleInputs["10000"].Id },
            CannotUpdateIds = new[] { fixture.ModuleInputs["10000"].Id },
            CannotDeleteIds = new[] { fixture.ModuleInputs["10000"].Id },
            CannotCreateParentIds = new[] { fixture.Modules["1000"].Id }
        };
    }
}