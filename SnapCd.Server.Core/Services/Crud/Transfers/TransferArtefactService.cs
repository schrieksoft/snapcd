// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using System.Text;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Services.Crud.Transfers;

/// <summary>
/// What the source produces for the receiver. demonolith runs one root at a time and the two halves
/// never see each other's working directory, so the files travel through here.
///
/// Encrypted at rest with the service that encrypts state, because a fragment is raw state.
/// </summary>
public class TransferArtefactService
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly IStateEncryptionService _encryption;

    public TransferArtefactService(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        IStateEncryptionService encryption)
    {
        _dbContextFactory = dbContextFactory;
        _encryption = encryption;
    }

    /// <summary>The fragment and its meta, as the source's map wrote them.</summary>
    public async Task StoreSourceFragment(
        Guid transferId, Guid organizationId, string fragment, string meta)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var row = await Row(dbContext, transferId, organizationId);
        row.SourceFragmentCiphertext = Encrypt(fragment);
        row.SourceFragmentMetaCiphertext = Encrypt(meta);

        await dbContext.SaveChangesAsync();
    }

    /// <summary>The output values the counterpart's plan reads.</summary>
    public async Task StoreReceiverOutputs(Guid transferId, Guid organizationId, string outputs)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var row = await Row(dbContext, transferId, organizationId);
        row.ReceiverOutputsCiphertext = Encrypt(outputs);

        await dbContext.SaveChangesAsync();
    }

    /// <summary>The fragment and its meta, or nulls while the source has not produced them.</summary>
    public async Task<(string? Fragment, string? Meta)> ReadSourceFragment(
        Guid transferId, Guid organizationId)
    {
        var row = await Find(transferId, organizationId);

        return row is null
            ? (null, null)
            : (Decrypt(row.SourceFragmentCiphertext), Decrypt(row.SourceFragmentMetaCiphertext));
    }

    public async Task<string?> ReadReceiverOutputs(Guid transferId, Guid organizationId) =>
        Decrypt((await Find(transferId, organizationId))?.ReceiverOutputsCiphertext);

    /// <summary>Whether the source has produced the fragment the receiver waits for.</summary>
    public async Task<bool> HasSourceFragment(Guid transferId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        return await dbContext.TransferArtefacts.AsNoTracking()
            .AnyAsync(a => a.TransferId == transferId
                           && a.OrganizationId == organizationId
                           && a.SourceFragmentCiphertext != null);
    }

    /// <summary>Whether the half that produces the values has stored them.</summary>
    public async Task<bool> HasReceiverOutputs(Guid transferId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        return await dbContext.TransferArtefacts.AsNoTracking()
            .AnyAsync(a => a.TransferId == transferId
                           && a.OrganizationId == organizationId
                           && a.ReceiverOutputsCiphertext != null);
    }

    private async Task<TransferArtefact?> Find(Guid transferId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        return await dbContext.TransferArtefacts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.TransferId == transferId && a.OrganizationId == organizationId);
    }

    private static async Task<TransferArtefact> Row(
        SnapCdDbContext dbContext, Guid transferId, Guid organizationId)
    {
        var existing = await dbContext.TransferArtefacts
            .FirstOrDefaultAsync(a => a.TransferId == transferId && a.OrganizationId == organizationId);

        if (existing is not null) return existing;

        var row = new TransferArtefact
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            TransferId = transferId
        };

        dbContext.TransferArtefacts.Add(row);
        return row;
    }

    private string Encrypt(string value) =>
        Convert.ToBase64String(_encryption.Encrypt(Encoding.UTF8.GetBytes(value)));

    private string? Decrypt(string? ciphertext) =>
        ciphertext is null
            ? null
            : Encoding.UTF8.GetString(_encryption.Decrypt(Convert.FromBase64String(ciphertext)));
}
