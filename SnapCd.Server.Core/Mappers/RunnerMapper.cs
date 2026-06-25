// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.Runners;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class RunnerMapper
{
    public static Runner ToEntity(RunnerCreateDto dto, Guid organizationId)
    {
        return new Runner
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ServicePrincipalId = dto.ServicePrincipalId,
            Name = dto.Name,
            IsDisabled = dto.IsDisabled,
            AllowMultipleInstances = dto.AllowMultipleInstances,
            IsSuppliedToAllModules = dto.IsSuppliedToAllModules
        };
    }

    public static RunnerReadDto ToDto(Runner entity)
    {
        return new RunnerReadDto
        {
            Id = entity.Id,
            ServicePrincipalId = entity.ServicePrincipalId,
            Name = entity.Name,
            IsDisabled = entity.IsDisabled,
            AllowMultipleInstances = entity.AllowMultipleInstances,
            IsSuppliedToAllModules = entity.IsSuppliedToAllModules
        };
    }

    public static void UpdateEntity(Runner entity, RunnerUpdateDto dto)
    {
        entity.ServicePrincipalId = dto.ServicePrincipalId;
        entity.Name = dto.Name;
        entity.IsDisabled = dto.IsDisabled;
        entity.AllowMultipleInstances = dto.AllowMultipleInstances;
        entity.IsSuppliedToAllModules = dto.IsSuppliedToAllModules;
    }
}