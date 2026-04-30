using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.NamespaceSecrets;

public static class OrganizationOwnerNamespaceSecretTestEntities
{
    public static void Seed(Fixture fixture, SnapCdDbContext dbContext)
    {
        // Create NamespaceSecret entities for Update tests
        fixture.NamespaceSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_NamespaceSecret_User_Tests)}_UpdateCan"] =
            fixture.CreateTestNamespaceSecret($"{nameof(OrganizationOwner_NamespaceSecret_User_Tests)}_UpdateCan", fixture.Namespaces["000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.NamespaceSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_NamespaceSecret_ServicePrincipal_Tests)}_UpdateCan"] =
            fixture.CreateTestNamespaceSecret($"{nameof(OrganizationOwner_NamespaceSecret_ServicePrincipal_Tests)}_UpdateCan", fixture.Namespaces["000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.NamespaceSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_NamespaceSecret_GroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestNamespaceSecret($"{nameof(OrganizationOwner_NamespaceSecret_GroupMember_Tests)}_UpdateCan", fixture.Namespaces["000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.NamespaceSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_NamespaceSecret_NestedGroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestNamespaceSecret($"{nameof(OrganizationOwner_NamespaceSecret_NestedGroupMember_Tests)}_UpdateCan", fixture.Namespaces["000"].Id, fixture.Organizations["0"].Id, dbContext);

        // Create NamespaceSecret entities for Delete tests
        fixture.NamespaceSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_NamespaceSecret_User_Tests)}_DeleteCan"] =
            fixture.CreateTestNamespaceSecret($"{nameof(OrganizationOwner_NamespaceSecret_User_Tests)}_DeleteCan", fixture.Namespaces["000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.NamespaceSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_NamespaceSecret_ServicePrincipal_Tests)}_DeleteCan"] =
            fixture.CreateTestNamespaceSecret($"{nameof(OrganizationOwner_NamespaceSecret_ServicePrincipal_Tests)}_DeleteCan", fixture.Namespaces["000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.NamespaceSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_NamespaceSecret_GroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestNamespaceSecret($"{nameof(OrganizationOwner_NamespaceSecret_GroupMember_Tests)}_DeleteCan", fixture.Namespaces["000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.NamespaceSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_NamespaceSecret_NestedGroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestNamespaceSecret($"{nameof(OrganizationOwner_NamespaceSecret_NestedGroupMember_Tests)}_DeleteCan", fixture.Namespaces["000"].Id, fixture.Organizations["0"].Id, dbContext);
    }
}