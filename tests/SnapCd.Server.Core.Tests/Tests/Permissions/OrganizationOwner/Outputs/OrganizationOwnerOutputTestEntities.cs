using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.Outputs;

public static class OrganizationOwnerOutputTestEntities
{
    public static void Seed(Fixture fixture, SnapCdDbContext dbContext)
    {
        // Create OutputSets for Delete tests (one per test class)
        var userOutputSet = fixture.CreateTestOutputSet($"{nameof(OrganizationOwner_Output_User_Tests)}_OutputSet", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);
        var spOutputSet = fixture.CreateTestOutputSet($"{nameof(OrganizationOwner_Output_ServicePrincipal_Tests)}_OutputSet", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);
        var groupOutputSet = fixture.CreateTestOutputSet($"{nameof(OrganizationOwner_Output_GroupMember_Tests)}_OutputSet", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);
        var nestedGroupOutputSet = fixture.CreateTestOutputSet($"{nameof(OrganizationOwner_Output_NestedGroupMember_Tests)}_OutputSet", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id,
            dbContext);

        // Create Outputs for Delete tests (Output cannot be updated, so only DeleteCan entities are needed)
        // User tests
        fixture.OutputAdditionalTestEntities[$"{nameof(OrganizationOwner_Output_User_Tests)}_DeleteCan"] =
            fixture.CreateTestOutput($"{nameof(OrganizationOwner_Output_User_Tests)}_DeleteCan", userOutputSet.Id, fixture.Organizations["0"].Id, dbContext);

        // ServicePrincipal tests
        fixture.OutputAdditionalTestEntities[$"{nameof(OrganizationOwner_Output_ServicePrincipal_Tests)}_DeleteCan"] =
            fixture.CreateTestOutput($"{nameof(OrganizationOwner_Output_ServicePrincipal_Tests)}_DeleteCan", spOutputSet.Id, fixture.Organizations["0"].Id, dbContext);

        // GroupMember tests
        fixture.OutputAdditionalTestEntities[$"{nameof(OrganizationOwner_Output_GroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestOutput($"{nameof(OrganizationOwner_Output_GroupMember_Tests)}_DeleteCan", groupOutputSet.Id, fixture.Organizations["0"].Id, dbContext);

        // NestedGroupMember tests
        fixture.OutputAdditionalTestEntities[$"{nameof(OrganizationOwner_Output_NestedGroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestOutput($"{nameof(OrganizationOwner_Output_NestedGroupMember_Tests)}_DeleteCan", nestedGroupOutputSet.Id, fixture.Organizations["0"].Id, dbContext);
    }
}