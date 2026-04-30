using Microsoft.AspNetCore.Components;
using SnapCd.Server.Core.Services.Edition;
using SnapCd.Server.SelfHosted.UI.Dashboard.Components;

namespace SnapCd.Server.SelfHosted.Services;

public class ServerEditionNavProvider : IEditionNavProvider
{
    public RenderFragment? EditionNavItems => builder =>
    {
        builder.OpenComponent<ServerEditionNavItems>(0);
        builder.CloseComponent();
    };

    public RenderFragment? EditionAccountNavItems => null;
}
