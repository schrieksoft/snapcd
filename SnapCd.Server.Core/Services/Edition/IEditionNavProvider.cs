// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Components;

namespace SnapCd.Server.Core.Services.Edition;

public interface IEditionNavProvider
{
    /// <summary>Rendered at the very top of the nav, above the organization selector.</summary>
    RenderFragment? EditionTopNavItems => null;

    /// <summary>Rendered inside the "System" nav group (e.g. the self-hosted "License" link).</summary>
    RenderFragment? EditionSystemNavItems { get; }

    /// <summary>Rendered standalone, after the "System" nav group (e.g. the SaaS "Subscriptions" group).</summary>
    RenderFragment? EditionNavItems { get; }

    RenderFragment? EditionAccountNavItems { get; }
}
