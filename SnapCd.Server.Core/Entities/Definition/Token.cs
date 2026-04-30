using System.ComponentModel.DataAnnotations;
using OpenIddict.EntityFrameworkCore.Models;

namespace SnapCd.Server.Core.Entities.Definition;

public class Token : OpenIddictEntityFrameworkCoreToken<Guid, ServicePrincipal, Authorization>
{
    public override Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(255)] public string? Name { get; set; }
}