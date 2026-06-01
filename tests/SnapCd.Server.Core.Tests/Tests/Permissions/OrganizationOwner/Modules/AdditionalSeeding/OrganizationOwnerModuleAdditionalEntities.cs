// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.Modules.AdditionalSeeding;

/// <summary>
/// Seeds test-specific Update/Delete modules for OrganizationOwner Module tests.
/// Creates 8 modules total: 2 per test class (UpdateCan, DeleteCan) × 4 test classes.
/// </summary>
public static class OrganizationOwnerModuleTestEntities
{
    public static void Seed(Fixture fixture, SnapCdDbContext dbContext)
    {
        // Create Update/Delete modules for each of the 4 test classes
        // Using different namespace paths to avoid conflicts

        // User test class - uses namespace "000"
        fixture.ModuleAdditionalTestEntities[$"{nameof(OrganizationOwner_Module_User_Tests)}_UpdateCan"] =
            fixture.CreateTestModule($"{nameof(OrganizationOwner_Module_User_Tests)}_UpdateCan", "000", dbContext);
        fixture.ModuleAdditionalTestEntities[$"{nameof(OrganizationOwner_Module_User_Tests)}_DeleteCan"] =
            fixture.CreateTestModule($"{nameof(OrganizationOwner_Module_User_Tests)}_DeleteCan", "000", dbContext);

        // ServicePrincipal test class - uses namespace "001"
        fixture.ModuleAdditionalTestEntities[$"{nameof(OrganizationOwner_Module_ServicePrincipal_Tests)}_UpdateCan"] =
            fixture.CreateTestModule($"{nameof(OrganizationOwner_Module_ServicePrincipal_Tests)}_UpdateCan", "001", dbContext);
        fixture.ModuleAdditionalTestEntities[$"{nameof(OrganizationOwner_Module_ServicePrincipal_Tests)}_DeleteCan"] =
            fixture.CreateTestModule($"{nameof(OrganizationOwner_Module_ServicePrincipal_Tests)}_DeleteCan", "001", dbContext);

        // GroupMember test class - uses namespace "010"
        fixture.ModuleAdditionalTestEntities[$"{nameof(OrganizationOwner_Module_GroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestModule($"{nameof(OrganizationOwner_Module_GroupMember_Tests)}_UpdateCan", "010", dbContext);
        fixture.ModuleAdditionalTestEntities[$"{nameof(OrganizationOwner_Module_GroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestModule($"{nameof(OrganizationOwner_Module_GroupMember_Tests)}_DeleteCan", "010", dbContext);

        // NestedGroupMember test class - uses namespace "011"
        fixture.ModuleAdditionalTestEntities[$"{nameof(OrganizationOwner_Module_NestedGroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestModule($"{nameof(OrganizationOwner_Module_NestedGroupMember_Tests)}_UpdateCan", "011", dbContext);
        fixture.ModuleAdditionalTestEntities[$"{nameof(OrganizationOwner_Module_NestedGroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestModule($"{nameof(OrganizationOwner_Module_NestedGroupMember_Tests)}_DeleteCan", "011", dbContext);
    }
}