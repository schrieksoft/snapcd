// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.NamespaceInputs;

public static class OrganizationOwnerNamespaceInputTestEntities
{
    public static void Seed(Fixture fixture, SnapCdDbContext dbContext)
    {
        // User tests
        fixture.NamespaceInputAdditionalTestEntities[$"{nameof(OrganizationOwner_NamespaceInput_User_Tests)}_UpdateCan"] =
            fixture.CreateTestNamespaceInput($"{nameof(OrganizationOwner_NamespaceInput_User_Tests)}_UpdateCan", fixture.Namespaces["000"].Id, fixture.Organizations["0"].Id, dbContext);
        fixture.NamespaceInputAdditionalTestEntities[$"{nameof(OrganizationOwner_NamespaceInput_User_Tests)}_DeleteCan"] =
            fixture.CreateTestNamespaceInput($"{nameof(OrganizationOwner_NamespaceInput_User_Tests)}_DeleteCan", fixture.Namespaces["000"].Id, fixture.Organizations["0"].Id, dbContext);

        // ServicePrincipal tests
        fixture.NamespaceInputAdditionalTestEntities[$"{nameof(OrganizationOwner_NamespaceInput_ServicePrincipal_Tests)}_UpdateCan"] =
            fixture.CreateTestNamespaceInput($"{nameof(OrganizationOwner_NamespaceInput_ServicePrincipal_Tests)}_UpdateCan", fixture.Namespaces["000"].Id, fixture.Organizations["0"].Id, dbContext);
        fixture.NamespaceInputAdditionalTestEntities[$"{nameof(OrganizationOwner_NamespaceInput_ServicePrincipal_Tests)}_DeleteCan"] =
            fixture.CreateTestNamespaceInput($"{nameof(OrganizationOwner_NamespaceInput_ServicePrincipal_Tests)}_DeleteCan", fixture.Namespaces["000"].Id, fixture.Organizations["0"].Id, dbContext);

        // GroupMember tests
        fixture.NamespaceInputAdditionalTestEntities[$"{nameof(OrganizationOwner_NamespaceInput_GroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestNamespaceInput($"{nameof(OrganizationOwner_NamespaceInput_GroupMember_Tests)}_UpdateCan", fixture.Namespaces["000"].Id, fixture.Organizations["0"].Id, dbContext);
        fixture.NamespaceInputAdditionalTestEntities[$"{nameof(OrganizationOwner_NamespaceInput_GroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestNamespaceInput($"{nameof(OrganizationOwner_NamespaceInput_GroupMember_Tests)}_DeleteCan", fixture.Namespaces["000"].Id, fixture.Organizations["0"].Id, dbContext);

        // NestedGroupMember tests
        fixture.NamespaceInputAdditionalTestEntities[$"{nameof(OrganizationOwner_NamespaceInput_NestedGroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestNamespaceInput($"{nameof(OrganizationOwner_NamespaceInput_NestedGroupMember_Tests)}_UpdateCan", fixture.Namespaces["000"].Id, fixture.Organizations["0"].Id, dbContext);
        fixture.NamespaceInputAdditionalTestEntities[$"{nameof(OrganizationOwner_NamespaceInput_NestedGroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestNamespaceInput($"{nameof(OrganizationOwner_NamespaceInput_NestedGroupMember_Tests)}_DeleteCan", fixture.Namespaces["000"].Id, fixture.Organizations["0"].Id, dbContext);
    }
}