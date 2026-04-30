using Microsoft.IdentityModel.Tokens;

namespace SnapCd.Server.Core.Settings;

public class SecurityKeySettings
{
    public required SymmetricSecurityKey SymmetricEncryptionKey { get; set; }
    public required RsaSecurityKey RsaSigningPrivateKey { get; set; }
    public required RsaSecurityKey RsaSigningPublicKey { get; set; }
}