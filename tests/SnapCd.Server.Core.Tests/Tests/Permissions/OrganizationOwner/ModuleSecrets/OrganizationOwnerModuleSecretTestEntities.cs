using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.ModuleSecrets;

public static class OrganizationOwnerModuleSecretTestEntities
{
    public static void Seed(Fixture fixture, SnapCdDbContext dbContext)
    {
        // Create ModuleSecret entities for Update tests
        fixture.ModuleSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_ModuleSecret_User_Tests)}_UpdateCan"] =
            fixture.CreateTestModuleSecret($"{nameof(OrganizationOwner_ModuleSecret_User_Tests)}_UpdateCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.ModuleSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_ModuleSecret_ServicePrincipal_Tests)}_UpdateCan"] =
            fixture.CreateTestModuleSecret($"{nameof(OrganizationOwner_ModuleSecret_ServicePrincipal_Tests)}_UpdateCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.ModuleSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_ModuleSecret_GroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestModuleSecret($"{nameof(OrganizationOwner_ModuleSecret_GroupMember_Tests)}_UpdateCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.ModuleSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_ModuleSecret_NestedGroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestModuleSecret($"{nameof(OrganizationOwner_ModuleSecret_NestedGroupMember_Tests)}_UpdateCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        // Create ModuleSecret entities for Delete tests
        fixture.ModuleSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_ModuleSecret_User_Tests)}_DeleteCan"] =
            fixture.CreateTestModuleSecret($"{nameof(OrganizationOwner_ModuleSecret_User_Tests)}_DeleteCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.ModuleSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_ModuleSecret_ServicePrincipal_Tests)}_DeleteCan"] =
            fixture.CreateTestModuleSecret($"{nameof(OrganizationOwner_ModuleSecret_ServicePrincipal_Tests)}_DeleteCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.ModuleSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_ModuleSecret_GroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestModuleSecret($"{nameof(OrganizationOwner_ModuleSecret_GroupMember_Tests)}_DeleteCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.ModuleSecretAdditionalTestEntities[$"{nameof(OrganizationOwner_ModuleSecret_NestedGroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestModuleSecret($"{nameof(OrganizationOwner_ModuleSecret_NestedGroupMember_Tests)}_DeleteCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);
    }
}