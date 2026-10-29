// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Licensing.Models;
using SnapCd.Server.Core.Licensing.Services;

namespace SnapCd.JobRun;

/// <summary>
/// Lifts the Module cap for a run whose fixture needs more Modules than the community tier allows.
///
/// The quota gate reads the cap off the licence and refuses to create a job once the organization
/// is over it, which is correct product behaviour: a seeded organization plus the fanout's four
/// Modules exceeds ten. Substituting the provider keeps the gate itself intact rather than raising
/// the product's cap for everyone.
/// </summary>
public class ModuleCapForTheRun : ILicenseInfoProvider
{
    public Task<LicenseInfo> GetLicenseInfoAsync(Guid organizationId) =>
        Task.FromResult(new LicenseInfo
        {
            Tier = Tier.Community,
            IsValid = true,
            MaxModules = null
        });
}
