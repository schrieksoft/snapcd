// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.ModuleJobApprovals;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Mappers.Repositories;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Services.MaintenanceMode;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class ModuleJobApprovalRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<ModuleJobApprovalRepositorySettings> options)
{
    public ModuleJobApprovalRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleJobApprovalRepository(dbContext, principalProvider, bus, options);
    }
}

public class ModuleJobApprovalRepository : GenericRepository<ModuleJobApproval, ModuleJobApprovalReadDto, ModuleJobApprovalCreatedEvent, ModuleJobApprovalUpdatedEvent, ModuleJobApprovalDeletedEvent,
    ModuleJobApprovalRepositorySettings>
{
    public ModuleJobApprovalRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ModuleJobApprovalRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override ModuleJobApprovalReadDto MapToDto(ModuleJobApproval entity)
    {
        return ModuleJobApprovalMapper.ToDto(entity);
    }

    public override async Task ExecuteDelete(Guid id, Guid organizationId)
    {
        await MaintenanceGate.EnsureWriteAllowedAsync();

        var entity = DbContext.Set<ModuleJobApproval>()
            .FirstOrDefault(m => m.Id == id && m.OrganizationId == organizationId);

        if (entity == null) throw new EntityNotFoundException($"ModuleJobApproval with ID {id} not found.");

        var deletedEntity = CloneEntity(entity);

        // The job's own row carries the condition, so the delete and the check are one statement
        // and the saga cannot leave the approval gate between them.
        var deleted = await DbContext.Set<ModuleJobApproval>()
            .Where(a => a.Id == id
                        && a.OrganizationId == organizationId
                        && DbContext.ModuleJobs.Any(j => j.Id == a.ModuleJobId
                                                         && j.OrganizationId == organizationId
                                                         && j.WaitingForApproval == true))
            .ExecuteDeleteAsync();

        if (deleted == 0)
            throw new ApprovalNoLongerWithdrawableException(
                $"ModuleJobApproval with ID {id} cannot be withdrawn because its job is no longer waiting for approval.");

        DbContext.Entry(entity).State = EntityState.Detached;

        if (Options.Value.EmitDeleteEvents && !SuppressEvents)
        {
            await EnqueueOrPublish(() => Bus.Publish(
                EventMapper.ToDeleteEto<ModuleJobApproval, ModuleJobApprovalReadDto, ModuleJobApprovalDeletedEvent>(
                    deletedEntity, MapToDto, organizationId),
                publishContext => { publishContext.TimeToLive = Options.Value.EventTtl; }));
            await PublishAdditionalMessages(AdditionalDeleteMessages(deletedEntity));
        }
    }

    protected override Func<IQueryable<ModuleJobApproval>, IQueryable<ModuleJobApproval>> ByParentIdQueryModifier(Guid parentId)
    {
        return query => query.Where(e => e.ModuleJobId == parentId);
    }

    public async Task<List<ModuleJobApproval>> ListByJob(Guid moduleJobId, Guid organizationId)
    {
        return await DbContext.Set<ModuleJobApproval>()
            .Where(a => a.ModuleJobId == moduleJobId && a.OrganizationId == organizationId)
            .ToListAsync();
    }
}