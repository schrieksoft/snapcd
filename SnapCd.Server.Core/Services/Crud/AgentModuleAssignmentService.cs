// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.AgentModuleAssignments;
using SnapCd.Server.Core.Entities.Definition.AgentAssignments;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.AgentAssignments;
using SnapCd.Server.Core.Repositories.Organizations.Secured.AgentAssignments;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class AgentModuleAssignmentService : GenericCrudService<
    AgentModuleAssignment,
    AgentModuleAssignmentCreateDto,
    AgentModuleAssignmentUpdateDto,
    AgentModuleAssignmentReadDto,
    AgentModuleAssignmentSecuredRepository,
    AgentModuleAssignmentRepository,
    AgentModuleAssignmentCreatedEvent,
    AgentModuleAssignmentUpdatedEvent,
    AgentModuleAssignmentDeletedEvent,
    AgentModuleAssignmentRepositorySettings>
{
    public AgentModuleAssignmentService(
        AgentModuleAssignmentSecuredRepository securedRepository) : base(securedRepository)
    {
    }


    protected override AgentModuleAssignment MapToEntity(AgentModuleAssignmentCreateDto dto, Guid organizationId)
    {
        return AgentModuleAssignmentMapper.ToEntity(dto, organizationId);
    }

    protected override AgentModuleAssignmentReadDto MapToDto(AgentModuleAssignment entity)
    {
        return AgentModuleAssignmentMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(AgentModuleAssignment entity, AgentModuleAssignmentUpdateDto dto)
    {
        AgentModuleAssignmentMapper.UpdateEntity(entity, dto);
    }
}
