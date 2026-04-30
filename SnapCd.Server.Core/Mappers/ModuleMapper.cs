using SnapCd.Contracts.Dto.Modules;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class ModuleMapper
{
    public static Module ToEntity(ModuleCreateDto dto, Guid organizationId)
    {
        return new Module
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            NamespaceId = dto.NamespaceId,
            Name = dto.Name,
            SourceUrl = dto.SourceUrl,
            SourceRevision = dto.SourceRevision,
            SourceSubdirectory = dto.SourceSubdirectory,
            SourceType = dto.SourceType,
            SourceRevisionType = dto.SourceRevisionType,
            RunnerId = dto.RunnerId,
            RunnerInstanceName = dto.RunnerInstanceName,
            InitBeforeHook = dto.InitBeforeHook,
            InitAfterHook = dto.InitAfterHook,
            IgnoreNamespaceBackendConfigs = dto.IgnoreNamespaceBackendConfigs,
            IgnoreNamespaceExtraFiles = dto.IgnoreNamespaceExtraFiles,
            IgnoreNamespaceFlags = dto.IgnoreNamespaceFlags,
            IgnoreNamespaceHooks = dto.IgnoreNamespaceHooks,
            AutoUpgradeEnabled = dto.AutoUpgradeEnabled,
            AutoReconfigureEnabled = dto.AutoReconfigureEnabled,
            AutoMigrateEnabled = dto.AutoMigrateEnabled,
            CleanInitEnabled = dto.CleanInitEnabled,
            PlanBeforeHook = dto.PlanBeforeHook,
            PlanAfterHook = dto.PlanAfterHook,
            PlanDestroyBeforeHook = dto.PlanDestroyBeforeHook,
            PlanDestroyAfterHook = dto.PlanDestroyAfterHook,
            ApplyBeforeHook = dto.ApplyBeforeHook,
            ApplyAfterHook = dto.ApplyAfterHook,
            OutputBeforeHook = dto.OutputBeforeHook,
            OutputAfterHook = dto.OutputAfterHook,
            DestroyBeforeHook = dto.DestroyBeforeHook,
            DestroyAfterHook = dto.DestroyAfterHook,
            ValidateBeforeHook = dto.ValidateBeforeHook,
            ValidateAfterHook = dto.ValidateAfterHook,
            ApplyApprovalThreshold = dto.ApplyApprovalThreshold,
            DestroyApprovalThreshold = dto.DestroyApprovalThreshold,
            ApprovalTimeoutMinutes = dto.ApprovalTimeoutMinutes,
            Engine = dto.Engine,
            WaitForApplyDependencies = dto.WaitForApplyDependencies,
            WaitForDestroyDependencies = dto.WaitForDestroyDependencies,
            TriggerOnDefinitionChanged = dto.TriggerOnDefinitionChanged,
            TriggerOnUpstreamOutputChanged = dto.TriggerOnUpstreamOutputChanged,
            TriggerOnSourceChanged = dto.TriggerOnSourceChanged,
            TriggerOnSourceChangedNotification = dto.TriggerOnSourceChangedNotification,
            DriftCheckEnabled = dto.DriftCheckEnabled,
            DriftCheckIntervalMinutes = dto.DriftCheckIntervalMinutes,
        };
    }

    public static ModuleReadDto ToDto(Module entity)
    {
        return new ModuleReadDto
        {
            Id = entity.Id,
            NamespaceId = entity.NamespaceId,
            Name = entity.Name,
            SourceUrl = entity.SourceUrl,
            SourceRevision = entity.SourceRevision,
            SourceSubdirectory = entity.SourceSubdirectory,
            SourceType = entity.SourceType,
            SourceRevisionType = entity.SourceRevisionType,
            RunnerId = entity.RunnerId,
            RunnerInstanceName = entity.RunnerInstanceName,
            InitBeforeHook = entity.InitBeforeHook,
            InitAfterHook = entity.InitAfterHook,
            IgnoreNamespaceBackendConfigs = entity.IgnoreNamespaceBackendConfigs,
            IgnoreNamespaceExtraFiles = entity.IgnoreNamespaceExtraFiles,
            IgnoreNamespaceFlags = entity.IgnoreNamespaceFlags,
            IgnoreNamespaceHooks = entity.IgnoreNamespaceHooks,
            AutoUpgradeEnabled = entity.AutoUpgradeEnabled,
            AutoReconfigureEnabled = entity.AutoReconfigureEnabled,
            AutoMigrateEnabled = entity.AutoMigrateEnabled,
            CleanInitEnabled = entity.CleanInitEnabled,
            PlanBeforeHook = entity.PlanBeforeHook,
            PlanAfterHook = entity.PlanAfterHook,
            PlanDestroyBeforeHook = entity.PlanDestroyBeforeHook,
            PlanDestroyAfterHook = entity.PlanDestroyAfterHook,
            ApplyBeforeHook = entity.ApplyBeforeHook,
            ApplyAfterHook = entity.ApplyAfterHook,
            OutputBeforeHook = entity.OutputBeforeHook,
            OutputAfterHook = entity.OutputAfterHook,
            DestroyBeforeHook = entity.DestroyBeforeHook,
            DestroyAfterHook = entity.DestroyAfterHook,
            ValidateBeforeHook = entity.ValidateBeforeHook,
            ValidateAfterHook = entity.ValidateAfterHook,
            ApplyApprovalThreshold = entity.ApplyApprovalThreshold,
            DestroyApprovalThreshold = entity.DestroyApprovalThreshold,
            ApprovalTimeoutMinutes = entity.ApprovalTimeoutMinutes,
            Engine = entity.Engine,
            WaitForApplyDependencies = entity.WaitForApplyDependencies,
            WaitForDestroyDependencies = entity.WaitForDestroyDependencies,
            TriggerOnDefinitionChanged = entity.TriggerOnDefinitionChanged,
            TriggerOnUpstreamOutputChanged = entity.TriggerOnUpstreamOutputChanged,
            TriggerOnSourceChanged = entity.TriggerOnSourceChanged,
            TriggerOnSourceChangedNotification = entity.TriggerOnSourceChangedNotification,
            DriftCheckEnabled = entity.DriftCheckEnabled,
            DriftCheckIntervalMinutes = entity.DriftCheckIntervalMinutes,
        };
    }

    public static void UpdateEntity(Module entity, ModuleUpdateDto dto)
    {
        entity.NamespaceId = dto.NamespaceId;
        entity.Name = dto.Name;
        entity.SourceUrl = dto.SourceUrl;
        entity.SourceRevision = dto.SourceRevision;
        entity.SourceSubdirectory = dto.SourceSubdirectory;
        entity.SourceType = dto.SourceType;
        entity.SourceRevisionType = dto.SourceRevisionType;
        entity.RunnerId = dto.RunnerId;
        entity.RunnerInstanceName = dto.RunnerInstanceName;
        entity.InitBeforeHook = dto.InitBeforeHook;
        entity.InitAfterHook = dto.InitAfterHook;
        entity.IgnoreNamespaceBackendConfigs = dto.IgnoreNamespaceBackendConfigs;
        entity.IgnoreNamespaceExtraFiles = dto.IgnoreNamespaceExtraFiles;
        entity.IgnoreNamespaceFlags = dto.IgnoreNamespaceFlags;
        entity.IgnoreNamespaceHooks = dto.IgnoreNamespaceHooks;
        entity.AutoUpgradeEnabled = dto.AutoUpgradeEnabled;
        entity.AutoReconfigureEnabled = dto.AutoReconfigureEnabled;
        entity.AutoMigrateEnabled = dto.AutoMigrateEnabled;
        entity.CleanInitEnabled = dto.CleanInitEnabled;
        entity.PlanBeforeHook = dto.PlanBeforeHook;
        entity.PlanAfterHook = dto.PlanAfterHook;
        entity.PlanDestroyBeforeHook = dto.PlanDestroyBeforeHook;
        entity.PlanDestroyAfterHook = dto.PlanDestroyAfterHook;
        entity.ApplyBeforeHook = dto.ApplyBeforeHook;
        entity.ApplyAfterHook = dto.ApplyAfterHook;
        entity.OutputBeforeHook = dto.OutputBeforeHook;
        entity.OutputAfterHook = dto.OutputAfterHook;
        entity.DestroyBeforeHook = dto.DestroyBeforeHook;
        entity.DestroyAfterHook = dto.DestroyAfterHook;
        entity.ValidateBeforeHook = dto.ValidateBeforeHook;
        entity.ValidateAfterHook = dto.ValidateAfterHook;
        entity.ApplyApprovalThreshold = dto.ApplyApprovalThreshold;
        entity.DestroyApprovalThreshold = dto.DestroyApprovalThreshold;
        entity.ApprovalTimeoutMinutes = dto.ApprovalTimeoutMinutes;
        entity.Engine = dto.Engine;
        entity.WaitForApplyDependencies = dto.WaitForApplyDependencies;
        entity.WaitForDestroyDependencies = dto.WaitForDestroyDependencies;
        entity.TriggerOnDefinitionChanged = dto.TriggerOnDefinitionChanged;
        entity.TriggerOnUpstreamOutputChanged = dto.TriggerOnUpstreamOutputChanged;
        entity.TriggerOnSourceChanged = dto.TriggerOnSourceChanged;
        entity.TriggerOnSourceChangedNotification = dto.TriggerOnSourceChangedNotification;
        entity.DriftCheckEnabled = dto.DriftCheckEnabled;
        entity.DriftCheckIntervalMinutes = dto.DriftCheckIntervalMinutes;
    }
}