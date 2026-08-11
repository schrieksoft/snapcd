// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization.Base;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Interfaces;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;

public abstract class
    GenericRunnerChildRepository<TEntity, TDto, TCreateEvent, TUpdateEvent, TDeleteEvent, TSettings> : GenericRepository<TEntity, TDto, TCreateEvent, TUpdateEvent, TDeleteEvent, TSettings>
    where TEntity : class, IEntity, IRunnerChild
    where TCreateEvent : CreatedEvent<TDto>, new()
    where TUpdateEvent : UpdatedEvent<TDto>, new()
    where TDeleteEvent : DeletedEvent<TDto>, new()
    where TSettings : class, IEntitySettings
{
    public GenericRunnerChildRepository(SnapCdDbContext dbContext, IPrincipalProvider principalProvider, IPublishEndpoint bus, IOptions<TSettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override Func<IQueryable<TEntity>, IQueryable<TEntity>> ByParentIdQueryModifier(Guid runnerId)
    {
        return query => query.Where(e => e.RunnerId == runnerId);
    }
}