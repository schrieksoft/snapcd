// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.OutputSets;

public static class OrganizationOwnerOutputSetTestEntities
{
    public static void Seed(Fixture fixture, SnapCdDbContext dbContext)
    {
        // Create OutputSets for Delete tests (OutputSet cannot be updated, so only DeleteCan entities are needed)
        // User tests
        fixture.OutputSetAdditionalTestEntities[$"{nameof(OrganizationOwner_OutputSet_User_Tests)}_DeleteCan"] =
            fixture.CreateTestOutputSet($"{nameof(OrganizationOwner_OutputSet_User_Tests)}_DeleteCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        // ServicePrincipal tests
        fixture.OutputSetAdditionalTestEntities[$"{nameof(OrganizationOwner_OutputSet_ServicePrincipal_Tests)}_DeleteCan"] =
            fixture.CreateTestOutputSet($"{nameof(OrganizationOwner_OutputSet_ServicePrincipal_Tests)}_DeleteCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        // GroupMember tests
        fixture.OutputSetAdditionalTestEntities[$"{nameof(OrganizationOwner_OutputSet_GroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestOutputSet($"{nameof(OrganizationOwner_OutputSet_GroupMember_Tests)}_DeleteCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        // NestedGroupMember tests
        fixture.OutputSetAdditionalTestEntities[$"{nameof(OrganizationOwner_OutputSet_NestedGroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestOutputSet($"{nameof(OrganizationOwner_OutputSet_NestedGroupMember_Tests)}_DeleteCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);
    }
}