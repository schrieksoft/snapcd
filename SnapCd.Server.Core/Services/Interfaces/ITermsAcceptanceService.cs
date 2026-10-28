// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

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
