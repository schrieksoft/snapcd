// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Server.Core.Tests.Infrastructure;
using SnapCd.Server.Core.Tests.TestActions;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.Outputs;

[Collection("NewRoleBasedSharedFixture")]
public class OrganizationOwner_Output_User_Tests : TestBase
{
    public OrganizationOwner_Output_User_Tests(Fixture fixture)
        : base(fixture, CreateConfig(fixture))
    {
    }

    private static TestScenarioConfiguration CreateConfig(Fixture fixture)
    {
        return new TestScenarioConfiguration
        {
            PrincipalId = fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id,
            Discriminator = PrincipalDiscriminator.User,
            TestActionsFactory = (f, db) => new OutputTestActions(f, db),
            NamePrefix = nameof(OrganizationOwner_Output_User_Tests),
            CanGetIds = new[] { fixture.Outputs["00000"].Id, fixture.Outputs["00001"].Id },
            CanDeleteIds = new[] { fixture.OutputAdditionalTestEntities[$"{nameof(OrganizationOwner_Output_User_Tests)}_DeleteCan"].Id },
            CannotGetIds = new[] { fixture.Outputs["10000"].Id },
            CannotDeleteIds = new[] { fixture.Outputs["10000"].Id },
            // Output cannot be updated by anyone, and OrgOwner cannot create Outputs
            CanUpdateIds = Array.Empty<Guid>(),
            CanCreateParentIds = Array.Empty<Guid>(),
            CannotUpdateIds = Array.Empty<Guid>(),
            CannotCreateParentIds = Array.Empty<Guid>()
        };
    }
}