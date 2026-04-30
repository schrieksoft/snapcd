using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using SnapCd.Server.Core.Entities.Definition;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace SnapCd.Server.Core.Controllers.OpenIddict;

[Route("api")]
[ApiExplorerSettings(IgnoreApi = true)]
public class ResourceController : Controller
{
    private readonly UserManager<User> _userManager;

    public ResourceController(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("message")]
    public async Task<IActionResult> GetMessage()
    {
        var subject = User.GetClaim(Claims.Subject);

        if (subject == null)
            throw new Exception("subject is empty");

        var user = await _userManager.FindByIdAsync(subject);
        if (user is null)
        {
            var d = new Dictionary<string, string?>
            {
                [OpenIddictValidationAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                [OpenIddictValidationAspNetCoreConstants.Properties.ErrorDescription] =
                    "The specified access token is bound to an account that no longer exists."
            };

            return Challenge(
                authenticationSchemes: OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(d)
            );
        }

        return Content($"{user.UserName} has been successfully authenticated.");
    }
}