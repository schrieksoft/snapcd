namespace SnapCd.Server.Core.Settings;

public class DebuggingOptions
{
    /// <summary>
    /// When true AND a debugger is attached, the LicenseService returns a synthetic valid
    /// EnterpriseEdition license bypassing all DB and SaaS checks. Has no effect when no
    /// debugger is attached. Intended for local development only.
    /// </summary>
    public bool ForceEnterpriseLicenseWhenDebuggerAttached { get; set; }
}
