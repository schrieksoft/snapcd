using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Services;

public interface ITermsAcceptanceService
{
    string CurrentTermsVersion { get; }
    string TermsUrl { get; }
    string PrivacyUrl { get; }

    Task<TermsAcceptance> RecordAcceptanceAsync(Guid userId, string context, string? ipAddress = null, string? userAgent = null);
    Task<bool> HasAcceptedCurrentTermsAsync(Guid userId);
    Task<TermsAcceptance?> GetLatestAcceptanceAsync(Guid userId);
    Task<List<TermsAcceptance>> GetAcceptanceHistoryAsync(Guid userId);
    Task<bool> NeedsReacceptanceAsync(Guid userId);
}
