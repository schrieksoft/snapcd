// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Services.PrincipalProvider;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SnapCd.Server.Core.Services.Crud.Transfers;

public class TransferOpenerFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    TransferServiceFactory transferServiceFactory)
{
    public TransferOpener Create(IPrincipalProvider? principalProvider = null) =>
        new(dbFactory, transferServiceFactory.Create(principalProvider));
}

/// <summary>
/// Opens a transfer: two Modules, the refs they run against, and a request for the receiver's
/// consent. What moves is decided in the code - the transfer map is committed alongside it and read
/// by demonolith from each Module's own checkout, so nothing about it is held here.
/// </summary>
public class TransferOpener
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly TransferService _transferService;

    public TransferOpener(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        TransferService transferService)
    {
        _dbContextFactory = dbContextFactory;
        _transferService = transferService;
    }

    public async Task<Transfer> Open(
        Guid sourceModuleId,
        Guid receiverModuleId,
        Guid organizationId,
        string? proveRef = null,
        TransferScope scope = TransferScope.Both,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var source = await LoadModule(dbContext, sourceModuleId, organizationId);
        await LoadModule(dbContext, receiverModuleId, organizationId);

        if (sourceModuleId == receiverModuleId)
            throw new ManualJobNotAllowedException("A Module cannot transfer to itself.");

        var ref_ = string.IsNullOrWhiteSpace(proveRef) ? source.SourceRevision : proveRef;

        var transfer = await _transferService.Create(
            sourceModuleId, organizationId, receiverModuleId, ref_, scope);

        // A source-only transfer never touches the receiver, so there is nothing to consent to.
        if (scope != TransferScope.SourceOnly)
            await _transferService.AskReceiverFor(transfer.Id, organizationId);

        return transfer;
    }

    private static async Task<Module> LoadModule(SnapCdDbContext dbContext, Guid moduleId, Guid organizationId)
    {
        var module = await dbContext.Modules.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == moduleId && m.OrganizationId == organizationId);

        if (module == null)
            throw new EntityNotFoundException($"Module '{moduleId}' not found");

        return module;
    }
}
