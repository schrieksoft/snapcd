using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.OrganizationOwner.Stacks;

/// <summary>
/// Seeds test-specific Stack entities for OrganizationOwner role tests.
/// These entities are dedicated for Update/Delete tests and will be modified during testing.
/// </summary>
public static class OrganizationOwnerStackTestEntities
{
    public static void Seed(Fixture fixture, SnapCdDbContext dbContext)
    {
        // User test class - creates 2 stacks for Update and Delete
        fixture.StackAdditionalTestEntities[$"{nameof(OrganizationOwner_Stack_User_Tests)}_UpdateCan"] =
            fixture.CreateTestStack($"{nameof(OrganizationOwner_Stack_User_Tests)}_UpdateCan", fixture.Organizations["0"].Id, dbContext);
        fixture.StackAdditionalTestEntities[$"{nameof(OrganizationOwner_Stack_User_Tests)}_DeleteCan"] =
            fixture.CreateTestStack($"{nameof(OrganizationOwner_Stack_User_Tests)}_DeleteCan", fixture.Organizations["0"].Id, dbContext);

        // ServicePrincipal test class
        fixture.StackAdditionalTestEntities[$"{nameof(OrganizationOwner_Stack_ServicePrincipal_Tests)}_UpdateCan"] =
            fixture.CreateTestStack($"{nameof(OrganizationOwner_Stack_ServicePrincipal_Tests)}_UpdateCan", fixture.Organizations["0"].Id, dbContext);
        fixture.StackAdditionalTestEntities[$"{nameof(OrganizationOwner_Stack_ServicePrincipal_Tests)}_DeleteCan"] =
            fixture.CreateTestStack($"{nameof(OrganizationOwner_Stack_ServicePrincipal_Tests)}_DeleteCan", fixture.Organizations["0"].Id, dbContext);

        // GroupMember test class
        fixture.StackAdditionalTestEntities[$"{nameof(OrganizationOwner_Stack_GroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestStack($"{nameof(OrganizationOwner_Stack_GroupMember_Tests)}_UpdateCan", fixture.Organizations["0"].Id, dbContext);
        fixture.StackAdditionalTestEntities[$"{nameof(OrganizationOwner_Stack_GroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestStack($"{nameof(OrganizationOwner_Stack_GroupMember_Tests)}_DeleteCan", fixture.Organizations["0"].Id, dbContext);

        // NestedGroupMember test class
        fixture.StackAdditionalTestEntities[$"{nameof(OrganizationOwner_Stack_NestedGroupMember_Tests)}_UpdateCan"] =
            fixture.CreateTestStack($"{nameof(OrganizationOwner_Stack_NestedGroupMember_Tests)}_UpdateCan", fixture.Organizations["0"].Id, dbContext);
        fixture.StackAdditionalTestEntities[$"{nameof(OrganizationOwner_Stack_NestedGroupMember_Tests)}_DeleteCan"] =
            fixture.CreateTestStack($"{nameof(OrganizationOwner_Stack_NestedGroupMember_Tests)}_DeleteCan", fixture.Organizations["0"].Id, dbContext);
    }
}