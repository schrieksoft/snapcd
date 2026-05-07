using SnapCd.Server.Core.Licensing.Models;

namespace SnapCd.Server.Core.Licensing;

/// <summary>
/// Thrown by runtime feature gates (vault factory, job creation) when an operation requires
/// a licence-gated feature that the current tier does not include. Surfaces through the
/// existing controller / job-failure pipelines so the user sees a clear "licence required"
/// message rather than a stack trace.
/// </summary>
public class LicenceFeatureUnavailableException : InvalidOperationException
{
    public Feature Feature { get; }

    public LicenceFeatureUnavailableException(Feature feature, string message)
        : base(message)
    {
        Feature = feature;
    }
}
