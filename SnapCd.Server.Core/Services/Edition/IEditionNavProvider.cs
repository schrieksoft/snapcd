using Microsoft.AspNetCore.Components;

namespace SnapCd.Server.Core.Services.Edition;

public interface IEditionNavProvider
{
    RenderFragment? EditionNavItems { get; }
    RenderFragment? EditionAccountNavItems { get; }
}
