// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.RunnerModuleAssignments;
using SnapCd.Server.Core.Entities.Definition.RunnerAssignments;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RunnerAssignments;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RunnerAssignments;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class RunnerModuleAssignmentService : GenericCrudService<
    RunnerModuleAssignment,
    RunnerModuleAssignmentCreateDto,
    RunnerModuleAssignmentUpdateDto,
    RunnerModuleAssignmentReadDto,
    RunnerModuleAssignmentSecuredRepository,
    RunnerModuleAssignmentRepository,
    RunnerModuleAssignmentCreatedEvent,
    RunnerModuleAssignmentUpdatedEvent,
    RunnerModuleAssignmentDeletedEvent,
    RunnerModuleAssignmentRepositorySettings>
{
    public RunnerModuleAssignmentService(
        RunnerModuleAssignmentSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override RunnerModuleAssignment MapToEntity(RunnerModuleAssignmentCreateDto dto, Guid organizationId)
    {
        return RunnerModuleAssignmentMapper.ToEntity(dto, organizationId);
    }

    protected override RunnerModuleAssignmentReadDto MapToDto(RunnerModuleAssignment entity)
    {
        return RunnerModuleAssignmentMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(RunnerModuleAssignment entity, RunnerModuleAssignmentUpdateDto dto)
    {
        RunnerModuleAssignmentMapper.UpdateEntity(entity, dto);
    }
}