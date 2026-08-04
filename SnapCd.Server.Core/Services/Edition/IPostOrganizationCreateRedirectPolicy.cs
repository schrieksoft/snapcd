// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Services.Edition;

/// <summary>
/// Edition-specific redirect targets for organization onboarding.
/// </summary>
public interface IPostOrganizationCreateRedirectPolicy
{
    /// <summary>
    /// Where to send the user immediately after creating an organization, as the returnUrl of the
    /// organization-switch redirect. Null goes straight to the Dashboard.
    /// </summary>
    string? RedirectPath { get; }

    /// <summary>
    /// Where to send a user who opens a product page before the organization is activated.
    /// Null when activation cannot fail, in which case the gate never fires.
    /// </summary>
    string? NotActivatedRedirectPath => null;

    /// <summary>
    /// Where to send the user after creating an organization when they arrived with the intent
    /// to pay (Tier=Paid). Falls back to <see cref="RedirectPath"/> when null.
    /// </summary>
    string? PaidTierRedirectPath => null;
}
