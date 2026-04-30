using SnapCd.Contracts.Dto.Namespaces;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class NamespaceMapper
{
    public static Namespace ToEntity(NamespaceCreateDto dto, Guid organizationId)
    {
        return new Namespace
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            StackId = dto.StackId ?? Guid.Empty,
            Name = dto.Name,
            DefaultInitBeforeHook = dto.DefaultInitBeforeHook,
            DefaultInitAfterHook = dto.DefaultInitAfterHook,
            DefaultAutoUpgradeEnabled = dto.DefaultAutoUpgradeEnabled,
            DefaultAutoReconfigureEnabled = dto.DefaultAutoReconfigureEnabled,
            DefaultAutoMigrateEnabled = dto.DefaultAutoMigrateEnabled,
            DefaultCleanInitEnabled = dto.DefaultCleanInitEnabled,
            DefaultPlanBeforeHook = dto.DefaultPlanBeforeHook,
            DefaultPlanAfterHook = dto.DefaultPlanAfterHook,
            DefaultPlanDestroyBeforeHook = dto.DefaultPlanDestroyBeforeHook,
            DefaultPlanDestroyAfterHook = dto.DefaultPlanDestroyAfterHook,
            DefaultApplyBeforeHook = dto.DefaultApplyBeforeHook,
            DefaultApplyAfterHook = dto.DefaultApplyAfterHook,
            DefaultOutputBeforeHook = dto.DefaultOutputBeforeHook,
            DefaultOutputAfterHook = dto.DefaultOutputAfterHook,
            DefaultDestroyBeforeHook = dto.DefaultDestroyBeforeHook,
            DefaultDestroyAfterHook = dto.DefaultDestroyAfterHook,
            DefaultValidateBeforeHook = dto.DefaultValidateBeforeHook,
            DefaultValidateAfterHook = dto.DefaultValidateAfterHook,
            DefaultApplyApprovalThreshold = dto.DefaultApplyApprovalThreshold,
            DefaultDestroyApprovalThreshold = dto.DefaultDestroyApprovalThreshold,
            DefaultApprovalTimeoutMinutes = dto.DefaultApprovalTimeoutMinutes,
            TriggerBehaviourOnModified = dto.TriggerBehaviourOnModified,
            DefaultEngine = dto.DefaultEngine,
            DefaultDriftCheckEnabled = dto.DefaultDriftCheckEnabled,
            DefaultDriftCheckIntervalMinutes = dto.DefaultDriftCheckIntervalMinutes,
        };
    }

    public static NamespaceReadDto ToDto(Namespace entity)
    {
        return new NamespaceReadDto
        {
            Id = entity.Id,
            StackId = entity.StackId,
            Name = entity.Name,
            DefaultInitBeforeHook = entity.DefaultInitBeforeHook,
            DefaultInitAfterHook = entity.DefaultInitAfterHook,
            DefaultAutoUpgradeEnabled = entity.DefaultAutoUpgradeEnabled,
            DefaultAutoReconfigureEnabled = entity.DefaultAutoReconfigureEnabled,
            DefaultAutoMigrateEnabled = entity.DefaultAutoMigrateEnabled,
            DefaultCleanInitEnabled = entity.DefaultCleanInitEnabled,
            DefaultPlanBeforeHook = entity.DefaultPlanBeforeHook,
            DefaultPlanAfterHook = entity.DefaultPlanAfterHook,
            DefaultPlanDestroyBeforeHook = entity.DefaultPlanDestroyBeforeHook,
            DefaultPlanDestroyAfterHook = entity.DefaultPlanDestroyAfterHook,
            DefaultApplyBeforeHook = entity.DefaultApplyBeforeHook,
            DefaultApplyAfterHook = entity.DefaultApplyAfterHook,
            DefaultOutputBeforeHook = entity.DefaultOutputBeforeHook,
            DefaultOutputAfterHook = entity.DefaultOutputAfterHook,
            DefaultDestroyBeforeHook = entity.DefaultDestroyBeforeHook,
            DefaultDestroyAfterHook = entity.DefaultDestroyAfterHook,
            DefaultValidateBeforeHook = entity.DefaultValidateBeforeHook,
            DefaultValidateAfterHook = entity.DefaultValidateAfterHook,
            DefaultApplyApprovalThreshold = entity.DefaultApplyApprovalThreshold,
            DefaultDestroyApprovalThreshold = entity.DefaultDestroyApprovalThreshold,
            DefaultApprovalTimeoutMinutes = entity.DefaultApprovalTimeoutMinutes,
            TriggerBehaviourOnModified = entity.TriggerBehaviourOnModified,
            DefaultEngine = entity.DefaultEngine,
            DefaultDriftCheckEnabled = entity.DefaultDriftCheckEnabled,
            DefaultDriftCheckIntervalMinutes = entity.DefaultDriftCheckIntervalMinutes,
        };
    }

    public static void UpdateEntity(Namespace entity, NamespaceUpdateDto dto)
    {
        entity.StackId = dto.StackId ?? entity.StackId;
        entity.Name = dto.Name;
        entity.DefaultInitBeforeHook = dto.DefaultInitBeforeHook;
        entity.DefaultInitAfterHook = dto.DefaultInitAfterHook;
        entity.DefaultAutoUpgradeEnabled = dto.DefaultAutoUpgradeEnabled;
        entity.DefaultAutoReconfigureEnabled = dto.DefaultAutoReconfigureEnabled;
        entity.DefaultAutoMigrateEnabled = dto.DefaultAutoMigrateEnabled;
        entity.DefaultCleanInitEnabled = dto.DefaultCleanInitEnabled;
        entity.DefaultPlanBeforeHook = dto.DefaultPlanBeforeHook;
        entity.DefaultPlanAfterHook = dto.DefaultPlanAfterHook;
        entity.DefaultPlanDestroyBeforeHook = dto.DefaultPlanDestroyBeforeHook;
        entity.DefaultPlanDestroyAfterHook = dto.DefaultPlanDestroyAfterHook;
        entity.DefaultApplyBeforeHook = dto.DefaultApplyBeforeHook;
        entity.DefaultApplyAfterHook = dto.DefaultApplyAfterHook;
        entity.DefaultOutputBeforeHook = dto.DefaultOutputBeforeHook;
        entity.DefaultOutputAfterHook = dto.DefaultOutputAfterHook;
        entity.DefaultDestroyBeforeHook = dto.DefaultDestroyBeforeHook;
        entity.DefaultDestroyAfterHook = dto.DefaultDestroyAfterHook;
        entity.DefaultValidateBeforeHook = dto.DefaultValidateBeforeHook;
        entity.DefaultValidateAfterHook = dto.DefaultValidateAfterHook;
        entity.DefaultApplyApprovalThreshold = dto.DefaultApplyApprovalThreshold;
        entity.DefaultDestroyApprovalThreshold = dto.DefaultDestroyApprovalThreshold;
        entity.DefaultApprovalTimeoutMinutes = dto.DefaultApprovalTimeoutMinutes;
        entity.TriggerBehaviourOnModified = dto.TriggerBehaviourOnModified;
        entity.DefaultEngine = dto.DefaultEngine;
        entity.DefaultDriftCheckEnabled = dto.DefaultDriftCheckEnabled;
        entity.DefaultDriftCheckIntervalMinutes = dto.DefaultDriftCheckIntervalMinutes;
    }
}