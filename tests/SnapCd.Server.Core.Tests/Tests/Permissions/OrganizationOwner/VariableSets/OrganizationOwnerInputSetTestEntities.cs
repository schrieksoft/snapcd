// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.VariableSets;

public static class OrganizationOwnerVariableSetTestEntities
{
    public static void Seed(Fixture fixture, SnapCdDbContext dbContext)
    {
        // Create VariableSet entities for Update tests (though VariableSet is immutable, we still test the permission check)
        fixture.VariableSetAdditionalTestEntities[$"{nameof(OrganizationOwner_VariableSet_User_Tests)}_UpdateCan"] =
            fixture.CreateTestVariableSet($"{nameof(OrganizationOwner_VariableSet_User_Tests)}_UpdateCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.VariableSetAdditionalTestEntities[$"{nameof(OrganizationOwner_VariableSet_ServicePrincipal_Tests)}_UpdateCan"] =
            fixture.CreateTestVariableSet($"{nameof(OrganizationOwner_VariableSet_ServicePrincipal_Tests)}_UpdateCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.VariableSetAdditionalTestEntities[$"{nameof(OrganizationOwner_VariableSet_GroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestVariableSet($"{nameof(OrganizationOwner_VariableSet_GroupMember_Tests)}_UpdateCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.VariableSetAdditionalTestEntities[$"{nameof(OrganizationOwner_VariableSet_NestedGroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestVariableSet($"{nameof(OrganizationOwner_VariableSet_NestedGroupMember_Tests)}_UpdateCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        // Create VariableSet entities for Delete tests
        fixture.VariableSetAdditionalTestEntities[$"{nameof(OrganizationOwner_VariableSet_User_Tests)}_DeleteCan"] =
            fixture.CreateTestVariableSet($"{nameof(OrganizationOwner_VariableSet_User_Tests)}_DeleteCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.VariableSetAdditionalTestEntities[$"{nameof(OrganizationOwner_VariableSet_ServicePrincipal_Tests)}_DeleteCan"] =
            fixture.CreateTestVariableSet($"{nameof(OrganizationOwner_VariableSet_ServicePrincipal_Tests)}_DeleteCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.VariableSetAdditionalTestEntities[$"{nameof(OrganizationOwner_VariableSet_GroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestVariableSet($"{nameof(OrganizationOwner_VariableSet_GroupMember_Tests)}_DeleteCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        fixture.VariableSetAdditionalTestEntities[$"{nameof(OrganizationOwner_VariableSet_NestedGroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestVariableSet($"{nameof(OrganizationOwner_VariableSet_NestedGroupMember_Tests)}_DeleteCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);
    }
}