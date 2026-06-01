// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Services;

public interface IUserQuotaProvider
{
    /// <summary>
    /// Max number of organizations the given user is allowed to own.
    /// NoOp (self-hosted) returns unbounded; SaaS combines settings + per-user override.
    /// </summary>
    Task<int> GetOrganizationQuotaAsync(Guid userId, CancellationToken ct = default);
}

public class NoOpUserQuotaProvider : IUserQuotaProvider
{
    public Task<int> GetOrganizationQuotaAsync(Guid userId, CancellationToken ct = default) =>
        Task.FromResult(int.MaxValue);
}
