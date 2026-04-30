using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.UserModuleRoleAssignments;

public static class OrganizationOwnerUserModuleRoleAssignmentTestEntities
{
    public static void Seed(Fixture fixture, SnapCdDbContext dbContext)
    {
        // Create test users for role assignment Update/Delete tests
        var updateCanUser_User = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserModuleRoleAssignment_User_Tests)}_UpdateCan", fixture.Organizations["0"].Id, dbContext);
        var deleteCanUser_User = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserModuleRoleAssignment_User_Tests)}_DeleteCan", fixture.Organizations["0"].Id, dbContext);

        var updateCanUser_SP = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserModuleRoleAssignment_ServicePrincipal_Tests)}_UpdateCan", fixture.Organizations["0"].Id, dbContext);
        var deleteCanUser_SP = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserModuleRoleAssignment_ServicePrincipal_Tests)}_DeleteCan", fixture.Organizations["0"].Id, dbContext);

        var updateCanUser_Group = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserModuleRoleAssignment_GroupMember_Tests)}_UpdateCan", fixture.Organizations["0"].Id, dbContext);
        var deleteCanUser_Group = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserModuleRoleAssignment_GroupMember_Tests)}_DeleteCan", fixture.Organizations["0"].Id, dbContext);

        var updateCanUser_Nested = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserModuleRoleAssignment_NestedGroupMember_Tests)}_UpdateCan", fixture.Organizations["0"].Id, dbContext);
        var deleteCanUser_Nested = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserModuleRoleAssignment_NestedGroupMember_Tests)}_DeleteCan", fixture.Organizations["0"].Id, dbContext);

        // Create UserModuleRoleAssignment entities for Update tests
        fixture.UserModuleRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserModuleRoleAssignment_User_Tests)}_UpdateCan"] =
            fixture.CreateTestUserModuleRoleAssignment(updateCanUser_User.Id, fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.UserModuleRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserModuleRoleAssignment_ServicePrincipal_Tests)}_UpdateCan"] =
            fixture.CreateTestUserModuleRoleAssignment(updateCanUser_SP.Id, fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.UserModuleRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserModuleRoleAssignment_GroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestUserModuleRoleAssignment(updateCanUser_Group.Id, fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.UserModuleRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserModuleRoleAssignment_NestedGroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestUserModuleRoleAssignment(updateCanUser_Nested.Id, fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        // Create UserModuleRoleAssignment entities for Delete tests
        fixture.UserModuleRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserModuleRoleAssignment_User_Tests)}_DeleteCan"] =
            fixture.CreateTestUserModuleRoleAssignment(deleteCanUser_User.Id, fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.UserModuleRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserModuleRoleAssignment_ServicePrincipal_Tests)}_DeleteCan"] =
            fixture.CreateTestUserModuleRoleAssignment(deleteCanUser_SP.Id, fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.UserModuleRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserModuleRoleAssignment_GroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestUserModuleRoleAssignment(deleteCanUser_Group.Id, fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.UserModuleRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserModuleRoleAssignment_NestedGroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestUserModuleRoleAssignment(deleteCanUser_Nested.Id, fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);
    }
}