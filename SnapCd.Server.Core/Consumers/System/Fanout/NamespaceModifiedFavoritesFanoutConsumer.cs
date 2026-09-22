// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.Notification;

namespace SnapCd.Server.Core.Consumers.System.Fanout;

/// <summary>
/// Refreshes the nav menu for users who starred this namespace. Favorite links carry its name
/// in their label and href, and resolve only when the menu loads.
/// </summary>
public class NamespaceModifiedFavoritesFanoutConsumer : IConsumer<NamespaceModifiedEvent>
{
    private readonly SnapCdDbContext _dbContext;
    private readonly FavoritesModifiedNotificationService _notificationService;

    public NamespaceModifiedFavoritesFanoutConsumer(
        SnapCdDbContext dbContext,
        FavoritesModifiedNotificationService notificationService)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
    }

    public async Task Consume(ConsumeContext<NamespaceModifiedEvent> context)
    {
        var userIds = await _dbContext.UserFavorites
            .Where(f => f.TargetType == FavoriteTargetType.Namespace && f.TargetId == context.Message.Id)
            .Select(f => f.UserId)
            .Distinct()
            .ToListAsync(context.CancellationToken);

        if (userIds.Count > 0)
            await _notificationService.Notify(userIds);
    }
}
