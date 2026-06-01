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
using SnapCd.Contracts.Dto.Secrets;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.Secrets;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Secrets;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured.Secrets.Scoped;

public class SecretSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<SecretRepositorySettings> secretOptions,
    IOptions<StackSecretRepositorySettings> stackOptions,
    IOptions<NamespaceSecretRepositorySettings> namespaceOptions,
    IOptions<ModuleSecretRepositorySettings> moduleOptions)
{
    public SecretSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());

        // Create a single DbContext shared by all repositories
        var dbContext = dbFactory.CreateDbContext();

        // Create the SecretSecuredRepository with all three secured repositories
        return new SecretSecuredRepository(
            new SecretRepository(dbContext, principalProvider, bus, secretOptions),
            principalProvider,
            new StackSecretSecuredRepository(new StackSecretRepository(dbContext, principalProvider, bus, stackOptions), principalProvider),
            new NamespaceSecretSecuredRepository(new NamespaceSecretRepository(dbContext, principalProvider, bus, namespaceOptions), principalProvider),
            new ModuleSecretSecuredRepository(new ModuleSecretRepository(dbContext, principalProvider, bus, moduleOptions), principalProvider));
    }
}

public class SecretSecuredRepository : GenericSecuredRepository<
    Secret,
    SecretDto,
    SecretRepository,
    SecretCreatedEvent,
    SecretUpdatedEvent,
    SecretDeletedEvent,
    SecretRepositorySettings>
{
    private readonly StackSecretSecuredRepository _stackSecuredRepository;
    private readonly NamespaceSecretSecuredRepository _namespaceSecuredRepository;
    private readonly ModuleSecretSecuredRepository _moduleSecuredRepository;

    public SecretSecuredRepository(
        SecretRepository repository,
        IPrincipalProvider principalProvider,
        StackSecretSecuredRepository stackSecuredRepository,
        NamespaceSecretSecuredRepository namespaceSecuredRepository,
        ModuleSecretSecuredRepository moduleSecuredRepository)
        : base(repository, principalProvider)
    {
        _stackSecuredRepository = stackSecuredRepository;
        _namespaceSecuredRepository = namespaceSecuredRepository;
        _moduleSecuredRepository = moduleSecuredRepository;
    }

    public async Task<Secret> GetByName(string name, Guid organizationId,
        Func<IQueryable<Secret>, IQueryable<Secret>>? include = null)
    {
        var secret = await Repository.GetByName(name, include);

        if (!CanRead(secret.Id, organizationId))
            throw new PrincipalNotAuthorizedException($"Access denied to Secret {secret.Id}");

        return secret;
    }

    public async Task<List<Secret>> ListByIds(List<Guid> ids, Guid organizationId)
    {
        var secrets = await Repository.ListByIds(ids, organizationId);

        foreach (var secret in secrets)
            if (!CanRead(secret.Id, organizationId))
                throw new PrincipalNotAuthorizedException($"Access denied to Secret {secret.Id}");

        return secrets;
    }

    public override IQueryable<Secret> CreateQuery(Guid organizationId)
    {
        var stackSecrets = _stackSecuredRepository.CreateQuery(organizationId).Cast<Secret>();
        var namespaceSecrets = _namespaceSecuredRepository.CreateQuery(organizationId).Cast<Secret>();
        var moduleSecrets = _moduleSecuredRepository.CreateQuery(organizationId).Cast<Secret>();

        return stackSecrets
            .Concat(namespaceSecrets)
            .Concat(moduleSecrets);
    }

    public override IQueryable<Secret> ReadQuery(Guid organizationId)
    {
        var stackSecrets = _stackSecuredRepository.ReadQuery(organizationId).Cast<Secret>();
        var namespaceSecrets = _namespaceSecuredRepository.ReadQuery(organizationId).Cast<Secret>();
        var moduleSecrets = _moduleSecuredRepository.ReadQuery(organizationId).Cast<Secret>();

        return stackSecrets
            .Concat(namespaceSecrets)
            .Concat(moduleSecrets);
    }

    public override IQueryable<Secret> UpdateQuery(Guid organizationId)
    {
        var stackSecrets = _stackSecuredRepository.UpdateQuery(organizationId).Cast<Secret>();
        var namespaceSecrets = _namespaceSecuredRepository.UpdateQuery(organizationId).Cast<Secret>();
        var moduleSecrets = _moduleSecuredRepository.UpdateQuery(organizationId).Cast<Secret>();

        return stackSecrets
            .Concat(namespaceSecrets)
            .Concat(moduleSecrets);
    }

    public override IQueryable<Secret> DeleteQuery(Guid organizationId)
    {
        var stackSecrets = _stackSecuredRepository.DeleteQuery(organizationId).Cast<Secret>();
        var namespaceSecrets = _namespaceSecuredRepository.DeleteQuery(organizationId).Cast<Secret>();
        var moduleSecrets = _moduleSecuredRepository.DeleteQuery(organizationId).Cast<Secret>();

        return stackSecrets
            .Concat(namespaceSecrets)
            .Concat(moduleSecrets);
    }

    public override bool CanCreate(Guid parentId, Guid organizationId)
    {
        // For Secret, parentId represents the scope-specific parent (StackId, NamespaceId, or ModuleId)
        // We need to check all three possible parent types since we don't know the concrete type at this point
        // The caller should ensure the correct parentId is passed based on the Secret type being created
        return _stackSecuredRepository.CanCreate(parentId, organizationId) ||
               _namespaceSecuredRepository.CanCreate(parentId, organizationId) ||
               _moduleSecuredRepository.CanCreate(parentId, organizationId);
    }

    public override bool CanRead(Guid id, Guid organizationId)
    {
        return _stackSecuredRepository.CanRead(id, organizationId) ||
               _namespaceSecuredRepository.CanRead(id, organizationId) ||
               _moduleSecuredRepository.CanRead(id, organizationId);
    }

    public override bool CanUpdate(Guid id, Guid organizationId)
    {
        return _stackSecuredRepository.CanUpdate(id, organizationId) ||
               _namespaceSecuredRepository.CanUpdate(id, organizationId) ||
               _moduleSecuredRepository.CanUpdate(id, organizationId);
    }

    public override bool CanDelete(Guid id, Guid organizationId)
    {
        return _stackSecuredRepository.CanDelete(id, organizationId) ||
               _namespaceSecuredRepository.CanDelete(id, organizationId) ||
               _moduleSecuredRepository.CanDelete(id, organizationId);
    }

    public override string GetParentEntityName()
    {
        return "Organization";
    }

    public override void Dispose()
    {
        _stackSecuredRepository.Dispose();
        _namespaceSecuredRepository.Dispose();
        _moduleSecuredRepository.Dispose();
        base.Dispose();
    }
}