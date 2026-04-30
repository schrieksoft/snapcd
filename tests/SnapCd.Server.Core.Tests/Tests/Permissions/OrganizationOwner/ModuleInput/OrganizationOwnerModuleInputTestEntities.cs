using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.ModuleInput;

public static class OrganizationOwnerModuleInputTestEntities
{
    public static void Seed(Fixture fixture, SnapCdDbContext dbContext)
    {
        // User tests
        fixture.ModuleInputAdditionalTestEntities[$"{nameof(OrganizationOwner_ModuleInput_User_Tests)}_UpdateCan"] =
            fixture.CreateTestModuleInput($"{nameof(OrganizationOwner_ModuleInput_User_Tests)}_UpdateCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);
        fixture.ModuleInputAdditionalTestEntities[$"{nameof(OrganizationOwner_ModuleInput_User_Tests)}_DeleteCan"] =
            fixture.CreateTestModuleInput($"{nameof(OrganizationOwner_ModuleInput_User_Tests)}_DeleteCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        // ServicePrincipal tests
        fixture.ModuleInputAdditionalTestEntities[$"{nameof(OrganizationOwner_ModuleInput_ServicePrincipal_Tests)}_UpdateCan"] =
            fixture.CreateTestModuleInput($"{nameof(OrganizationOwner_ModuleInput_ServicePrincipal_Tests)}_UpdateCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);
        fixture.ModuleInputAdditionalTestEntities[$"{nameof(OrganizationOwner_ModuleInput_ServicePrincipal_Tests)}_DeleteCan"] =
            fixture.CreateTestModuleInput($"{nameof(OrganizationOwner_ModuleInput_ServicePrincipal_Tests)}_DeleteCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        // GroupMember tests
        fixture.ModuleInputAdditionalTestEntities[$"{nameof(OrganizationOwner_ModuleInput_GroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestModuleInput($"{nameof(OrganizationOwner_ModuleInput_GroupMember_Tests)}_UpdateCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);
        fixture.ModuleInputAdditionalTestEntities[$"{nameof(OrganizationOwner_ModuleInput_GroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestModuleInput($"{nameof(OrganizationOwner_ModuleInput_GroupMember_Tests)}_DeleteCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        // NestedGroupMember tests
        fixture.ModuleInputAdditionalTestEntities[$"{nameof(OrganizationOwner_ModuleInput_NestedGroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestModuleInput($"{nameof(OrganizationOwner_ModuleInput_NestedGroupMember_Tests)}_UpdateCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);
        fixture.ModuleInputAdditionalTestEntities[$"{nameof(OrganizationOwner_ModuleInput_NestedGroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestModuleInput($"{nameof(OrganizationOwner_ModuleInput_NestedGroupMember_Tests)}_DeleteCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);
    }
}