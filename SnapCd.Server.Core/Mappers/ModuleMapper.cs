// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

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
            IgnoreNamespaceExtraFiles = dto.IgnoreNamespaceExtraFiles,
            IgnoreNamespaceFlags = dto.IgnoreNamespaceFlags,
            IgnoreNamespaceHooks = dto.IgnoreNamespaceHooks,
            CleanInitEnabled = dto.CleanInitEnabled,
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
            TriggerPathFilterEnabled = dto.TriggerPathFilterEnabled,
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
            IgnoreNamespaceExtraFiles = entity.IgnoreNamespaceExtraFiles,
            IgnoreNamespaceFlags = entity.IgnoreNamespaceFlags,
            IgnoreNamespaceHooks = entity.IgnoreNamespaceHooks,
            CleanInitEnabled = entity.CleanInitEnabled,
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
            TriggerPathFilterEnabled = entity.TriggerPathFilterEnabled,
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
        entity.IgnoreNamespaceExtraFiles = dto.IgnoreNamespaceExtraFiles;
        entity.IgnoreNamespaceFlags = dto.IgnoreNamespaceFlags;
        entity.IgnoreNamespaceHooks = dto.IgnoreNamespaceHooks;
        entity.CleanInitEnabled = dto.CleanInitEnabled;
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
        entity.TriggerPathFilterEnabled = dto.TriggerPathFilterEnabled;
        entity.DriftCheckEnabled = dto.DriftCheckEnabled;
        entity.DriftCheckIntervalMinutes = dto.DriftCheckIntervalMinutes;
    }
}
