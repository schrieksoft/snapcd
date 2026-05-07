using Microsoft.AspNetCore.Components;
using SnapCd.Server.Core.Services.Edition;
using SnapCd.Server.Host.UI.Dashboard.Components;

namespace SnapCd.Server.Host.Services;

public class ServerEditionNavProvider : IEditionNavProvider
{
    public RenderFragment? EditionNavItems => builder =>
    {
        builder.OpenComponent<ServerEditionNavItems>(0);
        builder.CloseComponent();
    };

    public RenderFragment? EditionAccountNavItems => null;
}
