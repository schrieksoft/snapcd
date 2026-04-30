namespace SnapCd.Server.Core.Misc.Constants;

public class ClaimTypeConstants
{
    public const string
        SubjectClaimType =
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"; //JwtRegisteredClaimNames.Sub;

    public const string PrincipalDiscriminatorClaimType = "principal_discriminator";

    public const string OrganizationClaimType = "organizations";

    public const string
        NameClaimType =
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
}