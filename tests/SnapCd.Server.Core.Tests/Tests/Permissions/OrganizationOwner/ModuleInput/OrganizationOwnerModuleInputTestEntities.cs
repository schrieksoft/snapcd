// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.ModuleInput;

public static class OrganizationOwnerModuleInputTestEntities
{
    public static void Seed(Fixture fixture, SnapCdDbContext dbContext)
    {
        // User tests
        fixture.ModuleInputAdditionalTestEntities[$"{nameof(OrganizationOwner_ModuleInput_User_Tests)}_UpdateCan"] =
            fixture.CreateTestModuleInput($"{nameof(OrganizationOwner_ModuleInput_User_Tests)}_UpdateCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);
        fixture.ModuleInputAdditionalTestEntities[$"{nameof(OrganizationOwner_ModuleInput_User_Tests)}_DeleteCan"] =
            fixture.CreateTestModuleInput($"{nameof(OrganizationOwner_ModuleInput_User_Tests)}_DeleteCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        // ServicePrincipal tests
        fixture.ModuleInputAdditionalTestEntities[$"{nameof(OrganizationOwner_ModuleInput_ServicePrincipal_Tests)}_UpdateCan"] =
            fixture.CreateTestModuleInput($"{nameof(OrganizationOwner_ModuleInput_ServicePrincipal_Tests)}_UpdateCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);
        fixture.ModuleInputAdditionalTestEntities[$"{nameof(OrganizationOwner_ModuleInput_ServicePrincipal_Tests)}_DeleteCan"] =
            fixture.CreateTestModuleInput($"{nameof(OrganizationOwner_ModuleInput_ServicePrincipal_Tests)}_DeleteCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        // GroupMember tests
        fixture.ModuleInputAdditionalTestEntities[$"{nameof(OrganizationOwner_ModuleInput_GroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestModuleInput($"{nameof(OrganizationOwner_ModuleInput_GroupMember_Tests)}_UpdateCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);
        fixture.ModuleInputAdditionalTestEntities[$"{nameof(OrganizationOwner_ModuleInput_GroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestModuleInput($"{nameof(OrganizationOwner_ModuleInput_GroupMember_Tests)}_DeleteCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);

        // NestedGroupMember tests
        fixture.ModuleInputAdditionalTestEntities[$"{nameof(OrganizationOwner_ModuleInput_NestedGroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestModuleInput($"{nameof(OrganizationOwner_ModuleInput_NestedGroupMember_Tests)}_UpdateCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);
        fixture.ModuleInputAdditionalTestEntities[$"{nameof(OrganizationOwner_ModuleInput_NestedGroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestModuleInput($"{nameof(OrganizationOwner_ModuleInput_NestedGroupMember_Tests)}_DeleteCan", fixture.Modules["0000"].Id, fixture.Organizations["0"].Id, dbContext);
    }
}