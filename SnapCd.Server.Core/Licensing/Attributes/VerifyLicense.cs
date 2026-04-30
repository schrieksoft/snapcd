namespace SnapCd.Server.Core.Licensing.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class VerifyLicense(string limitCategory) : Attribute
{
    public string LimitCategory { get; set; } = limitCategory;
}
