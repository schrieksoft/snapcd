// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Identity;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.Server.Core.Services;

public class UserLoginService
{
    private readonly UserManager<User> _userManager;
    private readonly IPrincipalProvider _principalProvider;

    public UserLoginService(
        UserManager<User> userManager,
        IPrincipalProvider principalProvider
    )
    {
        _userManager = userManager;
        _principalProvider = principalProvider;
    }

    /// <summary>
    /// Gets all external logins for the current user
    /// </summary>
    /// <returns>List of UserLoginInfo for the current user</returns>
    public async Task<IList<UserLoginInfo>> ListForCurrentUser()
    {
        var currentUserId = _principalProvider.GetSystemSubjectOrDefault();
        if (currentUserId == Guid.Empty) return new List<UserLoginInfo>();

        var user = await _userManager.FindByIdAsync(currentUserId.ToString());
        if (user == null) return new List<UserLoginInfo>();

        return await _userManager.GetLoginsAsync(user);
    }
}