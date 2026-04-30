using SnapCd.Contracts;

namespace SnapCd.Server.Core.Tests.TestActions;

/// <summary>
/// New interface for entity permission test actions with explicit positive and negative test methods.
/// This replaces the old ITestActions pattern of using shouldSucceed parameters.
/// Each operation has separate CanXxx and CannotXxx methods for clarity.
/// </summary>
public interface ITestActions
{
    /// <summary>
    /// Tests that a principal can list entities and the list contains the expected entities.
    /// Should successfully return a list containing all expectedEntityIds.
    /// </summary>
    Task CanList(Guid principalId, PrincipalDiscriminator discriminator, Guid[] expectedEntityIds);

    /// <summary>
    /// Tests that a principal cannot list certain entities (they should NOT appear in the list).
    /// Should return a list that does NOT contain any of the notExpectedEntityIds.
    /// </summary>
    Task CannotList(Guid principalId, PrincipalDiscriminator discriminator, Guid[] notExpectedEntityIds);

    /// <summary>
    /// Tests that a principal can get a specific entity.
    /// Should successfully retrieve and return the entity.
    /// </summary>
    Task CanGet(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId);

    /// <summary>
    /// Tests that a principal can update a specific entity.
    /// Should successfully update the entity's properties.
    /// </summary>
    Task CanUpdate(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId, string namePrefix);

    /// <summary>
    /// Tests that a principal can delete a specific entity.
    /// Should successfully delete the entity.
    /// </summary>
    Task CanDelete(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId);

    /// <summary>
    /// Tests that a principal can create an entity in a specific parent context.
    /// For Modules, parentId is a NamespaceId; for Namespaces, parentId is a StackId.
    /// Should successfully create the entity and clean up afterwards.
    /// </summary>
    Task CanCreate(Guid principalId, PrincipalDiscriminator discriminator, Guid parentId, string namePrefix);

    /// <summary>
    /// Tests that a principal CANNOT get a specific entity.
    /// Should throw PrincipalNotAuthorizedException.
    /// </summary>
    Task CannotGet(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId);

    /// <summary>
    /// Tests that a principal CANNOT update a specific entity.
    /// Should throw PrincipalNotAuthorizedException when attempting to update.
    /// </summary>
    Task CannotUpdate(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId);

    /// <summary>
    /// Tests that a principal CANNOT delete a specific entity.
    /// Should throw PrincipalNotAuthorizedException when attempting to delete.
    /// </summary>
    Task CannotDelete(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId);

    /// <summary>
    /// Tests that a principal CANNOT create an entity in a specific parent context.
    /// Should throw PrincipalNotAuthorizedException when attempting to create.
    /// </summary>
    Task CannotCreate(Guid principalId, PrincipalDiscriminator discriminator, Guid parentId);
}