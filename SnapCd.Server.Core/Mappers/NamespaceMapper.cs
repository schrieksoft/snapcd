// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

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
            DefaultCleanInitEnabled = dto.DefaultCleanInitEnabled,
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
            DefaultCleanInitEnabled = entity.DefaultCleanInitEnabled,
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
        entity.DefaultCleanInitEnabled = dto.DefaultCleanInitEnabled;
        entity.DefaultApplyApprovalThreshold = dto.DefaultApplyApprovalThreshold;
        entity.DefaultDestroyApprovalThreshold = dto.DefaultDestroyApprovalThreshold;
        entity.DefaultApprovalTimeoutMinutes = dto.DefaultApprovalTimeoutMinutes;
        entity.TriggerBehaviourOnModified = dto.TriggerBehaviourOnModified;
        entity.DefaultEngine = dto.DefaultEngine;
        entity.DefaultDriftCheckEnabled = dto.DefaultDriftCheckEnabled;
        entity.DefaultDriftCheckIntervalMinutes = dto.DefaultDriftCheckIntervalMinutes;
    }
}
