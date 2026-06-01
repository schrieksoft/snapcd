// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.Runners;

/// <summary>
/// Seeds test-specific Runner entities for OrganizationOwner role tests.
/// These entities are dedicated for Update/Delete tests and will be modified during testing.
/// </summary>
public static class OrganizationOwnerRunnerTestEntities
{
    public static void Seed(Fixture fixture, SnapCdDbContext dbContext)
    {
        // User test class - creates 2 Runners for Update and Delete
        fixture.RunnerAdditionalTestEntities[$"{nameof(OrganizationOwner_Runner_User_Tests)}_UpdateCan"] =
            fixture.CreateTestRunner($"{nameof(OrganizationOwner_Runner_User_Tests)}_UpdateCan", fixture.Organizations["0"].Id, dbContext);
        fixture.RunnerAdditionalTestEntities[$"{nameof(OrganizationOwner_Runner_User_Tests)}_DeleteCan"] =
            fixture.CreateTestRunner($"{nameof(OrganizationOwner_Runner_User_Tests)}_DeleteCan", fixture.Organizations["0"].Id, dbContext);

        // ServicePrincipal test class
        fixture.RunnerAdditionalTestEntities[$"{nameof(OrganizationOwner_Runner_ServicePrincipal_Tests)}_UpdateCan"] =
            fixture.CreateTestRunner($"{nameof(OrganizationOwner_Runner_ServicePrincipal_Tests)}_UpdateCan", fixture.Organizations["0"].Id, dbContext);
        fixture.RunnerAdditionalTestEntities[$"{nameof(OrganizationOwner_Runner_ServicePrincipal_Tests)}_DeleteCan"] =
            fixture.CreateTestRunner($"{nameof(OrganizationOwner_Runner_ServicePrincipal_Tests)}_DeleteCan", fixture.Organizations["0"].Id, dbContext);

        // GroupMember test class
        fixture.RunnerAdditionalTestEntities[$"{nameof(OrganizationOwner_Runner_GroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestRunner($"{nameof(OrganizationOwner_Runner_GroupMember_Tests)}_UpdateCan", fixture.Organizations["0"].Id, dbContext);
        fixture.RunnerAdditionalTestEntities[$"{nameof(OrganizationOwner_Runner_GroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestRunner($"{nameof(OrganizationOwner_Runner_GroupMember_Tests)}_DeleteCan", fixture.Organizations["0"].Id, dbContext);

        // NestedGroupMember test class
        fixture.RunnerAdditionalTestEntities[$"{nameof(OrganizationOwner_Runner_NestedGroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestRunner($"{nameof(OrganizationOwner_Runner_NestedGroupMember_Tests)}_UpdateCan", fixture.Organizations["0"].Id, dbContext);
        fixture.RunnerAdditionalTestEntities[$"{nameof(OrganizationOwner_Runner_NestedGroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestRunner($"{nameof(OrganizationOwner_Runner_NestedGroupMember_Tests)}_DeleteCan", fixture.Organizations["0"].Id, dbContext);
    }
}