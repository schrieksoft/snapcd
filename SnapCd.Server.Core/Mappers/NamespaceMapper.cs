// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.Namespaces;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Views.Metadata;

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
            DefaultCleanInitEnabled = dto.DefaultCleanInitEnabled,
            DefaultTriggerPathFilterEnabled = dto.DefaultTriggerPathFilterEnabled,
            DefaultApplyApprovalThreshold = dto.DefaultApplyApprovalThreshold,
            DefaultDestroyApprovalThreshold = dto.DefaultDestroyApprovalThreshold,
            DefaultStateMigrationApprovalThreshold = dto.DefaultStateMigrationApprovalThreshold,
            DefaultApprovalTimeoutMinutes = dto.DefaultApprovalTimeoutMinutes,
            TriggerBehaviourOnModified = dto.TriggerBehaviourOnModified,
            DefaultEngine = dto.DefaultEngine,
            DefaultDriftCheckEnabled = dto.DefaultDriftCheckEnabled,
            DefaultDriftCheckIntervalMinutes = dto.DefaultDriftCheckIntervalMinutes,
            ParentNamespaceId = dto.ParentNamespaceId,
            DefaultOutputSetDownstreamRestriction = dto.DefaultOutputSetDownstreamRestriction,
            DefaultOutputRestrictionExemption = dto.DefaultOutputRestrictionExemption,
            IgnoreInheritedExtraFiles = dto.IgnoreInheritedExtraFiles,
            IgnoreInheritedFlags = dto.IgnoreInheritedFlags,
            IgnoreInheritedHooks = dto.IgnoreInheritedHooks,
            IgnoreInheritedPolicies = dto.IgnoreInheritedPolicies,
            IgnoreInheritedMissions = dto.IgnoreInheritedMissions,
            IgnoreInheritedIntegrationEvents = dto.IgnoreInheritedIntegrationEvents,
            IgnoreInheritedTriggerPaths = dto.IgnoreInheritedTriggerPaths,
            OverrideOnCleanInitEnabled = dto.OverrideOnCleanInitEnabled,
            OverrideOnTriggerPathFilterEnabled = dto.OverrideOnTriggerPathFilterEnabled,
            OverrideOnApplyApprovalThreshold = dto.OverrideOnApplyApprovalThreshold,
            OverrideOnDestroyApprovalThreshold = dto.OverrideOnDestroyApprovalThreshold,
            OverrideOnStateMigrationApprovalThreshold = dto.OverrideOnStateMigrationApprovalThreshold,
            OverrideOnApprovalTimeoutMinutes = dto.OverrideOnApprovalTimeoutMinutes,
            OverrideOnEngine = dto.OverrideOnEngine,
            OverrideOnDriftCheckEnabled = dto.OverrideOnDriftCheckEnabled,
            OverrideOnDriftCheckIntervalMinutes = dto.OverrideOnDriftCheckIntervalMinutes,
            OverrideOnOutputSetDownstreamRestriction = dto.OverrideOnOutputSetDownstreamRestriction,
            OverrideOnOutputRestrictionExemption = dto.OverrideOnOutputRestrictionExemption,
        };
    }

    public static NamespaceReadDto ToDto(Namespace entity)
    {
        return new NamespaceReadDto
        {
            Id = entity.Id,
            StackId = entity.StackId,
            Name = entity.Name,
            DefaultCleanInitEnabled = entity.DefaultCleanInitEnabled,
            DefaultTriggerPathFilterEnabled = entity.DefaultTriggerPathFilterEnabled,
            DefaultApplyApprovalThreshold = entity.DefaultApplyApprovalThreshold,
            DefaultDestroyApprovalThreshold = entity.DefaultDestroyApprovalThreshold,
            DefaultStateMigrationApprovalThreshold = entity.DefaultStateMigrationApprovalThreshold,
            DefaultApprovalTimeoutMinutes = entity.DefaultApprovalTimeoutMinutes,
            TriggerBehaviourOnModified = entity.TriggerBehaviourOnModified,
            DefaultEngine = entity.DefaultEngine,
            DefaultDriftCheckEnabled = entity.DefaultDriftCheckEnabled,
            DefaultDriftCheckIntervalMinutes = entity.DefaultDriftCheckIntervalMinutes,
            ParentNamespaceId = entity.ParentNamespaceId,
            Number = entity.Number,
            DefaultOutputSetDownstreamRestriction = entity.DefaultOutputSetDownstreamRestriction,
            DefaultOutputRestrictionExemption = entity.DefaultOutputRestrictionExemption,
            IgnoreInheritedExtraFiles = entity.IgnoreInheritedExtraFiles,
            IgnoreInheritedFlags = entity.IgnoreInheritedFlags,
            IgnoreInheritedHooks = entity.IgnoreInheritedHooks,
            IgnoreInheritedPolicies = entity.IgnoreInheritedPolicies,
            IgnoreInheritedMissions = entity.IgnoreInheritedMissions,
            IgnoreInheritedIntegrationEvents = entity.IgnoreInheritedIntegrationEvents,
            IgnoreInheritedTriggerPaths = entity.IgnoreInheritedTriggerPaths,
            OverrideOnCleanInitEnabled = entity.OverrideOnCleanInitEnabled,
            OverrideOnTriggerPathFilterEnabled = entity.OverrideOnTriggerPathFilterEnabled,
            OverrideOnApplyApprovalThreshold = entity.OverrideOnApplyApprovalThreshold,
            OverrideOnDestroyApprovalThreshold = entity.OverrideOnDestroyApprovalThreshold,
            OverrideOnStateMigrationApprovalThreshold = entity.OverrideOnStateMigrationApprovalThreshold,
            OverrideOnApprovalTimeoutMinutes = entity.OverrideOnApprovalTimeoutMinutes,
            OverrideOnEngine = entity.OverrideOnEngine,
            OverrideOnDriftCheckEnabled = entity.OverrideOnDriftCheckEnabled,
            OverrideOnDriftCheckIntervalMinutes = entity.OverrideOnDriftCheckIntervalMinutes,
            OverrideOnOutputSetDownstreamRestriction = entity.OverrideOnOutputSetDownstreamRestriction,
            OverrideOnOutputRestrictionExemption = entity.OverrideOnOutputRestrictionExemption,
        };
    }

    public static NamespaceMetadataReadDto ToMetadataDto(NamespaceMetadata view)
    {
        return new NamespaceMetadataReadDto
        {
            Id = view.Id,
            Name = view.Name,
            StackId = view.StackId,
        };
    }

    public static void UpdateEntity(Namespace entity, NamespaceUpdateDto dto)
    {
        entity.StackId = dto.StackId ?? entity.StackId;
        entity.Name = dto.Name;
        entity.DefaultCleanInitEnabled = dto.DefaultCleanInitEnabled;
        entity.DefaultTriggerPathFilterEnabled = dto.DefaultTriggerPathFilterEnabled;
        entity.DefaultApplyApprovalThreshold = dto.DefaultApplyApprovalThreshold;
        entity.DefaultDestroyApprovalThreshold = dto.DefaultDestroyApprovalThreshold;
        entity.DefaultStateMigrationApprovalThreshold = dto.DefaultStateMigrationApprovalThreshold;
        entity.DefaultApprovalTimeoutMinutes = dto.DefaultApprovalTimeoutMinutes;
        entity.TriggerBehaviourOnModified = dto.TriggerBehaviourOnModified;
        entity.DefaultEngine = dto.DefaultEngine;
        entity.DefaultDriftCheckEnabled = dto.DefaultDriftCheckEnabled;
        entity.DefaultDriftCheckIntervalMinutes = dto.DefaultDriftCheckIntervalMinutes;
        entity.ParentNamespaceId = dto.ParentNamespaceId;
        entity.DefaultOutputSetDownstreamRestriction = dto.DefaultOutputSetDownstreamRestriction;
        entity.DefaultOutputRestrictionExemption = dto.DefaultOutputRestrictionExemption;
        entity.IgnoreInheritedExtraFiles = dto.IgnoreInheritedExtraFiles;
        entity.IgnoreInheritedFlags = dto.IgnoreInheritedFlags;
        entity.IgnoreInheritedHooks = dto.IgnoreInheritedHooks;
        entity.IgnoreInheritedPolicies = dto.IgnoreInheritedPolicies;
        entity.IgnoreInheritedMissions = dto.IgnoreInheritedMissions;
        entity.IgnoreInheritedIntegrationEvents = dto.IgnoreInheritedIntegrationEvents;
        entity.IgnoreInheritedTriggerPaths = dto.IgnoreInheritedTriggerPaths;
        entity.OverrideOnCleanInitEnabled = dto.OverrideOnCleanInitEnabled;
        entity.OverrideOnTriggerPathFilterEnabled = dto.OverrideOnTriggerPathFilterEnabled;
        entity.OverrideOnApplyApprovalThreshold = dto.OverrideOnApplyApprovalThreshold;
        entity.OverrideOnDestroyApprovalThreshold = dto.OverrideOnDestroyApprovalThreshold;
        entity.OverrideOnStateMigrationApprovalThreshold = dto.OverrideOnStateMigrationApprovalThreshold;
        entity.OverrideOnApprovalTimeoutMinutes = dto.OverrideOnApprovalTimeoutMinutes;
        entity.OverrideOnEngine = dto.OverrideOnEngine;
        entity.OverrideOnDriftCheckEnabled = dto.OverrideOnDriftCheckEnabled;
        entity.OverrideOnDriftCheckIntervalMinutes = dto.OverrideOnDriftCheckIntervalMinutes;
        entity.OverrideOnOutputSetDownstreamRestriction = dto.OverrideOnOutputSetDownstreamRestriction;
        entity.OverrideOnOutputRestrictionExemption = dto.OverrideOnOutputRestrictionExemption;
    }
}
