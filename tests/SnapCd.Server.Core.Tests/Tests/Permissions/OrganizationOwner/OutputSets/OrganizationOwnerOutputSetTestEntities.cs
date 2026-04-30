using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.OutputSets;

public static class OrganizationOwnerOutputSetTestEntities
{
    public static void Seed(Fixture fixture, SnapCdDbContext dbContext)
    {
        // Create OutputSets for Delete tests (OutputSet cannot be updated, so only DeleteCan entities are needed)
        // User tests
        fixture.OutputSetAdditionalTestEntities[$"{nameof(OrganizationOwner_OutputSet_User_Tests)}_DeleteCan"] =
            fixture.CreateTestOutputSet($"{nameof(OrganizationOwner_OutputSet_User_Tests)}_DeleteCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        // ServicePrincipal tests
        fixture.OutputSetAdditionalTestEntities[$"{nameof(OrganizationOwner_OutputSet_ServicePrincipal_Tests)}_DeleteCan"] =
            fixture.CreateTestOutputSet($"{nameof(OrganizationOwner_OutputSet_ServicePrincipal_Tests)}_DeleteCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        // GroupMember tests
        fixture.OutputSetAdditionalTestEntities[$"{nameof(OrganizationOwner_OutputSet_GroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestOutputSet($"{nameof(OrganizationOwner_OutputSet_GroupMember_Tests)}_DeleteCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        // NestedGroupMember tests
        fixture.OutputSetAdditionalTestEntities[$"{nameof(OrganizationOwner_OutputSet_NestedGroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestOutputSet($"{nameof(OrganizationOwner_OutputSet_NestedGroupMember_Tests)}_DeleteCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);
    }
}