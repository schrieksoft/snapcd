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