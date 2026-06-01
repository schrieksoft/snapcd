// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.Namespaces;

/// <summary>
/// Seeds test-specific Namespace entities for OrganizationOwner role tests.
/// These entities are dedicated for Update/Delete tests and will be modified during testing.
/// </summary>
public static class OrganizationOwnerNamespaceTestEntities
{
    public static void Seed(Fixture fixture, SnapCdDbContext dbContext)
    {
        // User test class - uses stack "00"
        fixture.NamespaceAdditionalTestEntities[$"{nameof(OrganizationOwner_Namespace_User_Tests)}_UpdateCan"] =
            fixture.CreateTestNamespace($"{nameof(OrganizationOwner_Namespace_User_Tests)}_UpdateCan", "00", dbContext);
        fixture.NamespaceAdditionalTestEntities[$"{nameof(OrganizationOwner_Namespace_User_Tests)}_DeleteCan"] =
            fixture.CreateTestNamespace($"{nameof(OrganizationOwner_Namespace_User_Tests)}_DeleteCan", "00", dbContext);

        // ServicePrincipal test class - uses stack "00"
        fixture.NamespaceAdditionalTestEntities[$"{nameof(OrganizationOwner_Namespace_ServicePrincipal_Tests)}_UpdateCan"] =
            fixture.CreateTestNamespace($"{nameof(OrganizationOwner_Namespace_ServicePrincipal_Tests)}_UpdateCan", "00", dbContext);
        fixture.NamespaceAdditionalTestEntities[$"{nameof(OrganizationOwner_Namespace_ServicePrincipal_Tests)}_DeleteCan"] =
            fixture.CreateTestNamespace($"{nameof(OrganizationOwner_Namespace_ServicePrincipal_Tests)}_DeleteCan", "00", dbContext);

        // GroupMember test class - uses stack "01"
        fixture.NamespaceAdditionalTestEntities[$"{nameof(OrganizationOwner_Namespace_GroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestNamespace($"{nameof(OrganizationOwner_Namespace_GroupMember_Tests)}_UpdateCan", "01", dbContext);
        fixture.NamespaceAdditionalTestEntities[$"{nameof(OrganizationOwner_Namespace_GroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestNamespace($"{nameof(OrganizationOwner_Namespace_GroupMember_Tests)}_DeleteCan", "01", dbContext);

        // NestedGroupMember test class - uses stack "01"
        fixture.NamespaceAdditionalTestEntities[$"{nameof(OrganizationOwner_Namespace_NestedGroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestNamespace($"{nameof(OrganizationOwner_Namespace_NestedGroupMember_Tests)}_UpdateCan", "01", dbContext);
        fixture.NamespaceAdditionalTestEntities[$"{nameof(OrganizationOwner_Namespace_NestedGroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestNamespace($"{nameof(OrganizationOwner_Namespace_NestedGroupMember_Tests)}_DeleteCan", "01", dbContext);
    }
}