// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.Variables;

public static class OrganizationOwnerInputTestEntities
{
    public static void Seed(Fixture fixture, SnapCdDbContext dbContext)
    {
        // First, ensure we have an VariableSet to attach Inputs to
        var testVariableSet = fixture.CreateTestVariableSet("OrganizationOwnerInputTestVariableSet", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        // Create Input entities for Update tests (though Input is immutable, we still test the permission check)
        fixture.InputAdditionalTestEntities[$"{nameof(OrganizationOwner_Input_User_Tests)}_UpdateCan"] =
            fixture.CreateTestInput($"{nameof(OrganizationOwner_Input_User_Tests)}_UpdateCan", testVariableSet.Id, fixture.Organizations["0"].Id, dbContext);

        fixture.InputAdditionalTestEntities[$"{nameof(OrganizationOwner_Input_ServicePrincipal_Tests)}_UpdateCan"] =
            fixture.CreateTestInput($"{nameof(OrganizationOwner_Input_ServicePrincipal_Tests)}_UpdateCan", testVariableSet.Id, fixture.Organizations["0"].Id, dbContext);

        fixture.InputAdditionalTestEntities[$"{nameof(OrganizationOwner_Input_GroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestInput($"{nameof(OrganizationOwner_Input_GroupMember_Tests)}_UpdateCan", testVariableSet.Id, fixture.Organizations["0"].Id, dbContext);

        fixture.InputAdditionalTestEntities[$"{nameof(OrganizationOwner_Input_NestedGroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestInput($"{nameof(OrganizationOwner_Input_NestedGroupMember_Tests)}_UpdateCan", testVariableSet.Id, fixture.Organizations["0"].Id, dbContext);

        // Create Input entities for Delete tests
        fixture.InputAdditionalTestEntities[$"{nameof(OrganizationOwner_Input_User_Tests)}_DeleteCan"] =
            fixture.CreateTestInput($"{nameof(OrganizationOwner_Input_User_Tests)}_DeleteCan", testVariableSet.Id, fixture.Organizations["0"].Id, dbContext);

        fixture.InputAdditionalTestEntities[$"{nameof(OrganizationOwner_Input_ServicePrincipal_Tests)}_DeleteCan"] =
            fixture.CreateTestInput($"{nameof(OrganizationOwner_Input_ServicePrincipal_Tests)}_DeleteCan", testVariableSet.Id, fixture.Organizations["0"].Id, dbContext);

        fixture.InputAdditionalTestEntities[$"{nameof(OrganizationOwner_Input_GroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestInput($"{nameof(OrganizationOwner_Input_GroupMember_Tests)}_DeleteCan", testVariableSet.Id, fixture.Organizations["0"].Id, dbContext);

        fixture.InputAdditionalTestEntities[$"{nameof(OrganizationOwner_Input_NestedGroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestInput($"{nameof(OrganizationOwner_Input_NestedGroupMember_Tests)}_DeleteCan", testVariableSet.Id, fixture.Organizations["0"].Id, dbContext);
    }
}