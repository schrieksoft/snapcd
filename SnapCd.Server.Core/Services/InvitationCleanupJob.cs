// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services;

public class InvitationCleanupJob
{
    private readonly SnapCdDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IOptions<InvitationSettings> _settings;
    private readonly ILogger<InvitationCleanupJob> _logger;

    public InvitationCleanupJob(
        SnapCdDbContext context,
        UserManager<User> userManager,
        IOptions<InvitationSettings> settings,
        ILogger<InvitationCleanupJob> logger)
    {
        _context = context;
        _userManager = userManager;
        _settings = settings;
        _logger = logger;
    }

    public async Task ExecuteJob()
    {
        if (!_settings.Value.AutoDeleteIncompleteUsers)
        {
            _logger.LogDebug("Auto-delete is disabled, skipping cleanup");
            return;
        }

        var expirationDays = _settings.Value.ExpirationDays;
        var cutoffDate = DateTime.UtcNow.AddDays(-expirationDays);

        _logger.LogInformation("Starting invitation cleanup for invitations older than {CutoffDate}", cutoffDate);

        // Find expired invitations
        var expiredOrgUsers = await _context.Set<OrganizationUser>()
            .Include(ou => ou.User)
            .Where(ou =>
                !ou.InvitationCompleted &&
                ou.InvitationExpirationDateTime != null &&
                ou.InvitationExpirationDateTime < DateTime.UtcNow)
            .ToListAsync();

        _logger.LogInformation("Found {Count} expired invitations to clean up", expiredOrgUsers.Count);

        int deletedUsers = 0;
        int deletedOrgUsers = 0;

        foreach (var orgUser in expiredOrgUsers)
        {
            try
            {
                // Delete incomplete user if they have no other organization memberships
                if (orgUser.User.IsRegistrationNotCompleted)
                {
                    var otherMemberships = await _context.Set<OrganizationUser>()
                        .CountAsync(ou => ou.UserId == orgUser.UserId && ou.Id != orgUser.Id);

                    if (otherMemberships == 0)
                    {
                        var result = await _userManager.DeleteAsync(orgUser.User);
                        if (result.Succeeded)
                        {
                            deletedUsers++;
                            _logger.LogInformation(
                                "Deleted incomplete user: {Email} (UserId: {UserId})",
                                orgUser.User.Email,
                                orgUser.UserId);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Failed to delete incomplete user {Email}: {Errors}",
                                orgUser.User.Email,
                                string.Join(", ", result.Errors.Select(e => e.Description)));
                        }
                    }
                }

                // Delete OrganizationUser record
                _context.Set<OrganizationUser>().Remove(orgUser);
                deletedOrgUsers++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error deleting expired invitation for user {Email}, org {OrgId}",
                    orgUser.User.Email,
                    orgUser.OrganizationId);
            }
        }

        if (deletedOrgUsers > 0)
        {
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation(
            "Invitation cleanup completed. Deleted {DeletedUsers} incomplete users and {DeletedOrgUsers} expired invitations",
            deletedUsers,
            deletedOrgUsers);
    }
}