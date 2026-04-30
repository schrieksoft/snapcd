using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.UserNamespaceRoleAssignments;

public static class OrganizationOwnerUserNamespaceRoleAssignmentTestEntities
{
    public static void Seed(Fixture fixture, SnapCdDbContext dbContext)
    {
        // Create test users for role assignment Update/Delete tests
        var updateCanUser_User = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserNamespaceRoleAssignment_User_Tests)}_UpdateCan", fixture.Organizations["0"].Id, dbContext);
        var deleteCanUser_User = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserNamespaceRoleAssignment_User_Tests)}_DeleteCan", fixture.Organizations["0"].Id, dbContext);

        var updateCanUser_SP = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserNamespaceRoleAssignment_ServicePrincipal_Tests)}_UpdateCan", fixture.Organizations["0"].Id, dbContext);
        var deleteCanUser_SP = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserNamespaceRoleAssignment_ServicePrincipal_Tests)}_DeleteCan", fixture.Organizations["0"].Id, dbContext);

        var updateCanUser_Group = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserNamespaceRoleAssignment_GroupMember_Tests)}_UpdateCan", fixture.Organizations["0"].Id, dbContext);
        var deleteCanUser_Group = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserNamespaceRoleAssignment_GroupMember_Tests)}_DeleteCan", fixture.Organizations["0"].Id, dbContext);

        var updateCanUser_Nested = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserNamespaceRoleAssignment_NestedGroupMember_Tests)}_UpdateCan", fixture.Organizations["0"].Id, dbContext);
        var deleteCanUser_Nested = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserNamespaceRoleAssignment_NestedGroupMember_Tests)}_DeleteCan", fixture.Organizations["0"].Id, dbContext);

        // Create UserNamespaceRoleAssignment entities for Update tests
        fixture.UserNamespaceRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserNamespaceRoleAssignment_User_Tests)}_UpdateCan"] =
            fixture.CreateTestUserNamespaceRoleAssignment(updateCanUser_User.Id, fixture.Namespaces["000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.UserNamespaceRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserNamespaceRoleAssignment_ServicePrincipal_Tests)}_UpdateCan"] =
            fixture.CreateTestUserNamespaceRoleAssignment(updateCanUser_SP.Id, fixture.Namespaces["000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.UserNamespaceRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserNamespaceRoleAssignment_GroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestUserNamespaceRoleAssignment(updateCanUser_Group.Id, fixture.Namespaces["000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.UserNamespaceRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserNamespaceRoleAssignment_NestedGroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestUserNamespaceRoleAssignment(updateCanUser_Nested.Id, fixture.Namespaces["000"].Id, fixture.Organizations["0"].Id, dbContext);

        // Create UserNamespaceRoleAssignment entities for Delete tests
        fixture.UserNamespaceRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserNamespaceRoleAssignment_User_Tests)}_DeleteCan"] =
            fixture.CreateTestUserNamespaceRoleAssignment(deleteCanUser_User.Id, fixture.Namespaces["000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.UserNamespaceRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserNamespaceRoleAssignment_ServicePrincipal_Tests)}_DeleteCan"] =
            fixture.CreateTestUserNamespaceRoleAssignment(deleteCanUser_SP.Id, fixture.Namespaces["000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.UserNamespaceRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserNamespaceRoleAssignment_GroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestUserNamespaceRoleAssignment(deleteCanUser_Group.Id, fixture.Namespaces["000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.UserNamespaceRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserNamespaceRoleAssignment_NestedGroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestUserNamespaceRoleAssignment(deleteCanUser_Nested.Id, fixture.Namespaces["000"].Id, fixture.Organizations["0"].Id, dbContext);
    }
}