// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.UserOrganizationRoleAssignments;

public static class OrganizationOwnerUserOrganizationRoleAssignmentTestEntities
{
    public static void Seed(Fixture fixture, SnapCdDbContext dbContext)
    {
        // Create test users for role assignment Update/Delete tests
        var updateCanUser_User = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserOrganizationRoleAssignment_User_Tests)}_UpdateCan", fixture.Organizations["0"].Id, dbContext);
        var deleteCanUser_User = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserOrganizationRoleAssignment_User_Tests)}_DeleteCan", fixture.Organizations["0"].Id, dbContext);

        var updateCanUser_SP = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserOrganizationRoleAssignment_ServicePrincipal_Tests)}_UpdateCan", fixture.Organizations["0"].Id, dbContext);
        var deleteCanUser_SP = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserOrganizationRoleAssignment_ServicePrincipal_Tests)}_DeleteCan", fixture.Organizations["0"].Id, dbContext);

        var updateCanUser_Group = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserOrganizationRoleAssignment_GroupMember_Tests)}_UpdateCan", fixture.Organizations["0"].Id, dbContext);
        var deleteCanUser_Group = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserOrganizationRoleAssignment_GroupMember_Tests)}_DeleteCan", fixture.Organizations["0"].Id, dbContext);

        var updateCanUser_Nested = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserOrganizationRoleAssignment_NestedGroupMember_Tests)}_UpdateCan", fixture.Organizations["0"].Id, dbContext);
        var deleteCanUser_Nested = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserOrganizationRoleAssignment_NestedGroupMember_Tests)}_DeleteCan", fixture.Organizations["0"].Id, dbContext);

        // Create UserOrganizationRoleAssignment entities for Update tests
        fixture.UserOrganizationRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserOrganizationRoleAssignment_User_Tests)}_UpdateCan"] =
            fixture.CreateTestUserOrganizationRoleAssignment(updateCanUser_User.Id, fixture.Organizations["0"].Id, dbContext);

        fixture.UserOrganizationRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserOrganizationRoleAssignment_ServicePrincipal_Tests)}_UpdateCan"] =
            fixture.CreateTestUserOrganizationRoleAssignment(updateCanUser_SP.Id, fixture.Organizations["0"].Id, dbContext);

        fixture.UserOrganizationRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserOrganizationRoleAssignment_GroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestUserOrganizationRoleAssignment(updateCanUser_Group.Id, fixture.Organizations["0"].Id, dbContext);

        fixture.UserOrganizationRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserOrganizationRoleAssignment_NestedGroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestUserOrganizationRoleAssignment(updateCanUser_Nested.Id, fixture.Organizations["0"].Id, dbContext);

        // Create UserOrganizationRoleAssignment entities for Delete tests
        fixture.UserOrganizationRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserOrganizationRoleAssignment_User_Tests)}_DeleteCan"] =
            fixture.CreateTestUserOrganizationRoleAssignment(deleteCanUser_User.Id, fixture.Organizations["0"].Id, dbContext);

        fixture.UserOrganizationRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserOrganizationRoleAssignment_ServicePrincipal_Tests)}_DeleteCan"] =
            fixture.CreateTestUserOrganizationRoleAssignment(deleteCanUser_SP.Id, fixture.Organizations["0"].Id, dbContext);

        fixture.UserOrganizationRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserOrganizationRoleAssignment_GroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestUserOrganizationRoleAssignment(deleteCanUser_Group.Id, fixture.Organizations["0"].Id, dbContext);

        fixture.UserOrganizationRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserOrganizationRoleAssignment_NestedGroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestUserOrganizationRoleAssignment(deleteCanUser_Nested.Id, fixture.Organizations["0"].Id, dbContext);
    }
}