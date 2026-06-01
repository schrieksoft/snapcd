// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

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
