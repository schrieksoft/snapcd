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
/// The files one participant's slice produces for another: state fragments and threaded output
/// values. They are encrypted at rest with the same service that encrypts state, because a fragment
/// is raw state, and they are deleted when the job ends however it ends.
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

    /// <summary>Stores a slice's file, replacing any earlier one of the same name for this job.</summary>
    public async Task Store(Guid jobId, Guid organizationId, string name, string content)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var ciphertext = Convert.ToBase64String(_encryption.Encrypt(Encoding.UTF8.GetBytes(content)));

        var existing = await dbContext.ManualModuleJobArtefacts
            .FirstOrDefaultAsync(a => a.JobId == jobId && a.Name == name && a.OrganizationId == organizationId);

        if (existing != null)
        {
            existing.Ciphertext = ciphertext;
        }
        else
        {
            dbContext.ManualModuleJobArtefacts.Add(new ManualModuleJobArtefact
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                JobId = jobId,
                Name = name,
                Ciphertext = ciphertext
            });
        }

        await dbContext.SaveChangesAsync();
    }

    /// <summary>The file's contents, or null when the job never produced it.</summary>
    public async Task<string?> Read(Guid jobId, Guid organizationId, string name)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var artefact = await dbContext.ManualModuleJobArtefacts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.JobId == jobId && a.Name == name && a.OrganizationId == organizationId);

        return artefact == null
            ? null
            : Encoding.UTF8.GetString(_encryption.Decrypt(Convert.FromBase64String(artefact.Ciphertext)));
    }

    /// <summary>
    /// Drops every artefact the job produced. Called from each terminal path, so a fragment does not
    /// outlive the job that needed it.
    /// </summary>
    public async Task<int> DeleteForJob(Guid jobId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        return await dbContext.ManualModuleJobArtefacts
            .Where(a => a.JobId == jobId && a.OrganizationId == organizationId)
            .ExecuteDeleteAsync();
    }
}
