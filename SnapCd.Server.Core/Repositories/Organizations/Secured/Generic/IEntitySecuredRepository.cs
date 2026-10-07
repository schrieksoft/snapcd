// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;

/// <summary>
/// The part of a secured repository a generic editor needs, without naming the repository's own type arguments.
/// </summary>
public interface IEntitySecuredRepository<TEntity> : IDisposable
    where TEntity : class, IEntity
{
    Task<TEntity> Get(
        Guid id,
        Guid organizationId,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryModifier = null);

    Task<int> Count(
        Guid organizationId,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryModifier = null);

    Task<List<TEntity>> List(
        Guid organizationId,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryModifier = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null);

    Task<List<TEntity>> ListByParentId(
        Guid parentId,
        Guid organizationId,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryModifier = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null);

    Task<TEntity> Create(TEntity entity, bool inTransaction = true);
    Task<TEntity> Update(TEntity entity, bool inTransaction = true);
    Task Delete(Guid id, Guid organizationId, bool inTransaction = true);

    bool CanRead(Guid id, Guid organizationId);
    bool CanReadMetadata(Guid id, Guid organizationId);
    bool CanCreate(Guid parentId, Guid organizationId);
    bool CanUpdate(Guid id, Guid organizationId);
    bool CanDelete(Guid id, Guid organizationId);
}

/// <summary>
/// Creates a secured repository for an editor that cannot name the concrete repository type.
/// </summary>
public interface IEntitySecuredRepositoryFactory<TEntity>
    where TEntity : class, IEntity
{
    IEntitySecuredRepository<TEntity> Create(IPrincipalProvider? principalProvider = null);
}
