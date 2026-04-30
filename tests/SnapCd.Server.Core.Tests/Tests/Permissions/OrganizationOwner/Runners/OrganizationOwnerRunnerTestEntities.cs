using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.Runners;

/// <summary>
/// Seeds test-specific Runner entities for OrganizationOwner role tests.
/// These entities are dedicated for Update/Delete tests and will be modified during testing.
/// </summary>
public static class OrganizationOwnerRunnerTestEntities
{
    public static void Seed(Fixture fixture, SnapCdDbContext dbContext)
    {
        // User test class - creates 2 Runners for Update and Delete
        fixture.RunnerAdditionalTestEntities[$"{nameof(OrganizationOwner_Runner_User_Tests)}_UpdateCan"] =
            fixture.CreateTestRunner($"{nameof(OrganizationOwner_Runner_User_Tests)}_UpdateCan", fixture.Organizations["0"].Id, dbContext);
        fixture.RunnerAdditionalTestEntities[$"{nameof(OrganizationOwner_Runner_User_Tests)}_DeleteCan"] =
            fixture.CreateTestRunner($"{nameof(OrganizationOwner_Runner_User_Tests)}_DeleteCan", fixture.Organizations["0"].Id, dbContext);

        // ServicePrincipal test class
        fixture.RunnerAdditionalTestEntities[$"{nameof(OrganizationOwner_Runner_ServicePrincipal_Tests)}_UpdateCan"] =
            fixture.CreateTestRunner($"{nameof(OrganizationOwner_Runner_ServicePrincipal_Tests)}_UpdateCan", fixture.Organizations["0"].Id, dbContext);
        fixture.RunnerAdditionalTestEntities[$"{nameof(OrganizationOwner_Runner_ServicePrincipal_Tests)}_DeleteCan"] =
            fixture.CreateTestRunner($"{nameof(OrganizationOwner_Runner_ServicePrincipal_Tests)}_DeleteCan", fixture.Organizations["0"].Id, dbContext);

        // GroupMember test class
        fixture.RunnerAdditionalTestEntities[$"{nameof(OrganizationOwner_Runner_GroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestRunner($"{nameof(OrganizationOwner_Runner_GroupMember_Tests)}_UpdateCan", fixture.Organizations["0"].Id, dbContext);
        fixture.RunnerAdditionalTestEntities[$"{nameof(OrganizationOwner_Runner_GroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestRunner($"{nameof(OrganizationOwner_Runner_GroupMember_Tests)}_DeleteCan", fixture.Organizations["0"].Id, dbContext);

        // NestedGroupMember test class
        fixture.RunnerAdditionalTestEntities[$"{nameof(OrganizationOwner_Runner_NestedGroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestRunner($"{nameof(OrganizationOwner_Runner_NestedGroupMember_Tests)}_UpdateCan", fixture.Organizations["0"].Id, dbContext);
        fixture.RunnerAdditionalTestEntities[$"{nameof(OrganizationOwner_Runner_NestedGroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestRunner($"{nameof(OrganizationOwner_Runner_NestedGroupMember_Tests)}_DeleteCan", fixture.Organizations["0"].Id, dbContext);
    }
}