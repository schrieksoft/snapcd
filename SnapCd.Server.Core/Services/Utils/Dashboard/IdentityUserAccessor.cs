// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Services.Dashboard;

public class IdentityUserAccessor(UserManager<User> userManager, IdentityRedirectManager redirectManager)
{
    public async Task<User> GetRequiredUserAsync(HttpContext context)
    {
        var user = await userManager.GetUserAsync(context.User);

        if (user is null) redirectManager.RedirectToWithStatus("Account/InvalidUser", $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);

        return user;
    }

    public async Task<User?> GetUserAsync(AuthenticationState authState)
    {
        if (authState.User.Identity?.IsAuthenticated == true) return await userManager.GetUserAsync(authState.User);
        return null;
    }

    public async Task<User> GetRequiredUserAsync(AuthenticationState authState)
    {
        var user = await GetUserAsync(authState);

        if (user is null) throw new InvalidOperationException($"Unable to load user with ID '{userManager.GetUserId(authState.User)}'.");

        return user;
    }
}