using OpenIddict.EntityFrameworkCore.Models;

namespace SnapCd.Server.Core.Entities.Definition;

public class Scope : OpenIddictEntityFrameworkCoreScope<Guid>
{
    public override Guid Id { get; set; } = Guid.NewGuid();
}