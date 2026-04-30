using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.StackSecrets;

public static class OrganizationOwnerStackSecretTestEntities
{
    public static void Seed(Fixture fixture, SnapCdDbContext dbContext)
    {
        // Create StackSecret entities for Update tests
        fixture.StackSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_StackSecret_User_Tests)}_UpdateCan"] =
            fixture.CreateTestStackSecret($"{nameof(OrganizationOwner_StackSecret_User_Tests)}_UpdateCan", fixture.Stacks["00"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.StackSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_StackSecret_ServicePrincipal_Tests)}_UpdateCan"] =
            fixture.CreateTestStackSecret($"{nameof(OrganizationOwner_StackSecret_ServicePrincipal_Tests)}_UpdateCan", fixture.Stacks["00"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.StackSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_StackSecret_GroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestStackSecret($"{nameof(OrganizationOwner_StackSecret_GroupMember_Tests)}_UpdateCan", fixture.Stacks["00"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.StackSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_StackSecret_NestedGroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestStackSecret($"{nameof(OrganizationOwner_StackSecret_NestedGroupMember_Tests)}_UpdateCan", fixture.Stacks["00"].Id, fixture.Organizations["0"].Id, dbContext);

        // Create StackSecret entities for Delete tests
        fixture.StackSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_StackSecret_User_Tests)}_DeleteCan"] =
            fixture.CreateTestStackSecret($"{nameof(OrganizationOwner_StackSecret_User_Tests)}_DeleteCan", fixture.Stacks["00"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.StackSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_StackSecret_ServicePrincipal_Tests)}_DeleteCan"] =
            fixture.CreateTestStackSecret($"{nameof(OrganizationOwner_StackSecret_ServicePrincipal_Tests)}_DeleteCan", fixture.Stacks["00"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.StackSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_StackSecret_GroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestStackSecret($"{nameof(OrganizationOwner_StackSecret_GroupMember_Tests)}_DeleteCan", fixture.Stacks["00"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.StackSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_StackSecret_NestedGroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestStackSecret($"{nameof(OrganizationOwner_StackSecret_NestedGroupMember_Tests)}_DeleteCan", fixture.Stacks["00"].Id, fixture.Organizations["0"].Id, dbContext);
    }
}