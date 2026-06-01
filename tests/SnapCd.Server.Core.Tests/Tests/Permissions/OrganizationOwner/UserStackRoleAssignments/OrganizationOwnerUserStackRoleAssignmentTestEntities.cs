// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.UserStackRoleAssignments;

public static class OrganizationOwnerUserStackRoleAssignmentTestEntities
{
    public static void Seed(Fixture fixture, SnapCdDbContext dbContext)
    {
        // Create test users for role assignment Update/Delete tests
        var updateCanUser_User = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserStackRoleAssignment_User_Tests)}_UpdateCan", fixture.Organizations["0"].Id, dbContext);
        var deleteCanUser_User = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserStackRoleAssignment_User_Tests)}_DeleteCan", fixture.Organizations["0"].Id, dbContext);

        var updateCanUser_SP = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserStackRoleAssignment_ServicePrincipal_Tests)}_UpdateCan", fixture.Organizations["0"].Id, dbContext);
        var deleteCanUser_SP = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserStackRoleAssignment_ServicePrincipal_Tests)}_DeleteCan", fixture.Organizations["0"].Id, dbContext);

        var updateCanUser_Group = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserStackRoleAssignment_GroupMember_Tests)}_UpdateCan", fixture.Organizations["0"].Id, dbContext);
        var deleteCanUser_Group = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserStackRoleAssignment_GroupMember_Tests)}_DeleteCan", fixture.Organizations["0"].Id, dbContext);

        var updateCanUser_Nested = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserStackRoleAssignment_NestedGroupMember_Tests)}_UpdateCan", fixture.Organizations["0"].Id, dbContext);
        var deleteCanUser_Nested = fixture.CreateTestUser($"{nameof(OrganizationOwner_UserStackRoleAssignment_NestedGroupMember_Tests)}_DeleteCan", fixture.Organizations["0"].Id, dbContext);

        // Create UserStackRoleAssignment entities for Update tests
        fixture.UserStackRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserStackRoleAssignment_User_Tests)}_UpdateCan"] =
            fixture.CreateTestUserStackRoleAssignment(updateCanUser_User.Id, fixture.Stacks["00"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.UserStackRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserStackRoleAssignment_ServicePrincipal_Tests)}_UpdateCan"] =
            fixture.CreateTestUserStackRoleAssignment(updateCanUser_SP.Id, fixture.Stacks["00"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.UserStackRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserStackRoleAssignment_GroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestUserStackRoleAssignment(updateCanUser_Group.Id, fixture.Stacks["00"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.UserStackRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserStackRoleAssignment_NestedGroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestUserStackRoleAssignment(updateCanUser_Nested.Id, fixture.Stacks["00"].Id, fixture.Organizations["0"].Id, dbContext);

        // Create UserStackRoleAssignment entities for Delete tests
        fixture.UserStackRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserStackRoleAssignment_User_Tests)}_DeleteCan"] =
            fixture.CreateTestUserStackRoleAssignment(deleteCanUser_User.Id, fixture.Stacks["00"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.UserStackRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserStackRoleAssignment_ServicePrincipal_Tests)}_DeleteCan"] =
            fixture.CreateTestUserStackRoleAssignment(deleteCanUser_SP.Id, fixture.Stacks["00"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.UserStackRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserStackRoleAssignment_GroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestUserStackRoleAssignment(deleteCanUser_Group.Id, fixture.Stacks["00"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.UserStackRoleAssignmentAdditionalTestEntities[$"{nameof(OrganizationOwner_UserStackRoleAssignment_NestedGroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestUserStackRoleAssignment(deleteCanUser_Nested.Id, fixture.Stacks["00"].Id, fixture.Organizations["0"].Id, dbContext);
    }
}