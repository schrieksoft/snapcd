using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Tests.TestActions;

namespace SnapCd.Server.Core.Tests.Infrastructure;

/// <summary>
/// Configuration for a scenario-based permission test.
/// Defines the principal, discriminator, test actions, and expected results.
/// All properties are direct values (not functions) for clarity and simplicity.
/// </summary>
public class TestScenarioConfiguration
{
    /// <summary>
    /// The principal ID to test with.
    /// Example: fixture.OrganizationRoleUsers[OrganizationRole.Owner].Id
    /// </summary>
    public required Guid PrincipalId { get; init; }

    /// <summary>
    /// The discriminator for the principal (User, ServicePrincipal, etc.).
    /// </summary>
    public required PrincipalDiscriminator Discriminator { get; init; }

    /// <summary>
    /// Factory function to create the test actions instance.
    /// Only this property remains a function since it needs DbContext which is created during test initialization.
    /// Example: (f, db) => new ModuleTestActionsNew(f, db)
    /// </summary>
    public required Func<Fixture, SnapCdDbContext, ITestActions> TestActionsFactory { get; init; }

    /// <summary>
    /// Name prefix for test entities created during testing.
    /// </summary>
    public required string NamePrefix { get; init; }

    // Collection-based configurations for testing positive and negative cases

    /// <summary>
    /// Entity IDs that the principal SHOULD be able to get.
    /// Example: new[] { fixture.TestModule.Id, fixture.OtherModule.Id }
    /// Use Array.Empty&lt;Guid&gt;() if no positive cases to test.
    /// </summary>
    public required Guid[] CanGetIds { get; init; }

    /// <summary>
    /// Entity IDs that the principal SHOULD NOT be able to get.
    /// Example: new[] { fixture.RestrictedModule.Id }
    /// Use Array.Empty&lt;Guid&gt;() if no negative cases to test.
    /// </summary>
    public required Guid[] CannotGetIds { get; init; }

    /// <summary>
    /// Entity IDs that the principal SHOULD be able to update.
    /// Example: new[] { fixture.TestModule.Id, fixture.OtherModule.Id }
    /// Use Array.Empty&lt;Guid&gt;() if no positive cases to test.
    /// </summary>
    public required Guid[] CanUpdateIds { get; init; }

    /// <summary>
    /// Entity IDs that the principal SHOULD NOT be able to update.
    /// Example: new[] { fixture.RestrictedModule.Id }
    /// Use Array.Empty&lt;Guid&gt;() if no negative cases to test.
    /// </summary>
    public required Guid[] CannotUpdateIds { get; init; }

    /// <summary>
    /// Entity IDs that the principal SHOULD be able to delete.
    /// Example: new[] { fixture.DeletableModule.Id }
    /// Use Array.Empty&lt;Guid&gt;() if no positive cases to test.
    /// </summary>
    public required Guid[] CanDeleteIds { get; init; }

    /// <summary>
    /// Entity IDs that the principal SHOULD NOT be able to delete.
    /// Example: new[] { fixture.ProtectedModule.Id }
    /// Use Array.Empty&lt;Guid&gt;() if no negative cases to test.
    /// </summary>
    public required Guid[] CannotDeleteIds { get; init; }

    /// <summary>
    /// Parent IDs where the principal SHOULD be able to create entities.
    /// For Modules, these are Namespace IDs; for Namespaces, these are Stack IDs.
    /// Example: new[] { fixture.TestNamespace.Id, fixture.OtherNamespace.Id }
    /// Use Array.Empty&lt;Guid&gt;() if no positive cases to test.
    /// </summary>
    public required Guid[] CanCreateParentIds { get; init; }

    /// <summary>
    /// Parent IDs where the principal SHOULD NOT be able to create entities.
    /// Example: new[] { fixture.RestrictedNamespace.Id }
    /// Use Array.Empty&lt;Guid&gt;() if no negative cases to test.
    /// </summary>
    public required Guid[] CannotCreateParentIds { get; init; }
}