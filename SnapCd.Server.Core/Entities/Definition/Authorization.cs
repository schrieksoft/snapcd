using System.Collections.ObjectModel;
using OpenIddict.EntityFrameworkCore.Models;

namespace SnapCd.Server.Core.Entities.Definition;

public class Authorization : OpenIddictEntityFrameworkCoreAuthorization<Guid, ServicePrincipal, Token>
{
    public override ICollection<Token> Tokens { get; } = new ObservableCollection<Token>();

    public override Guid Id { get; set; } = Guid.NewGuid();
}