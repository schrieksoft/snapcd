// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Services;

public class NoOpTermsAcceptanceService : ITermsAcceptanceService
{
    public string CurrentTermsVersion => "0000-00-00";
    public string TermsUrl => "/terms";
    public string PrivacyUrl => "/privacy";

    public Task<TermsAcceptance> RecordAcceptanceAsync(Guid userId, string context, string? ipAddress = null, string? userAgent = null) =>
        Task.FromResult(new TermsAcceptance
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TermsVersion = CurrentTermsVersion,
            AcceptanceContext = context,
            AcceptedDateTime = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

    public Task<bool> HasAcceptedCurrentTermsAsync(Guid userId) => Task.FromResult(true);
    public Task<TermsAcceptance?> GetLatestAcceptanceAsync(Guid userId) => Task.FromResult<TermsAcceptance?>(null);
    public Task<List<TermsAcceptance>> GetAcceptanceHistoryAsync(Guid userId) => Task.FromResult(new List<TermsAcceptance>());
    public Task<bool> NeedsReacceptanceAsync(Guid userId) => Task.FromResult(false);
}
