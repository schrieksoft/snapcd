// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Server.Core.Events.Steps.Base;

namespace SnapCd.Server.Core.Events.System;

public class SourceRefreshCompleted : StepRequestBase
{
    public required string SourceUrl { get; set; }
    public required string SourceRevision { get; set; }
    public SourceType SourceType { get; set; } = SourceType.Git;
    public SourceRevisionType SourceRevisionType { get; set; } = SourceRevisionType.Default;

    public required string DefinitiveRevision { get; set; }

    /// <summary>
    /// Tree hashes of the watched directories at DefinitiveRevision, present only when the runner answered a
    /// path-aware refresh (SourceRefreshCompletedV2). Null means head-only semantics.
    /// </summary>
    public List<PathHash>? PathHashes { get; set; }

    /// <summary>
    /// Discovered reference closures per watched root, when the runner ran snapcd-inspect at the refreshed
    /// revision. Null means no discovery — closures compose over declared paths only.
    /// </summary>
    public List<ModuleClosure>? ModuleClosures { get; set; }

    /// <summary>
    /// True when the refresh was dispatched by a SourceChanged notification; notification-triggered Modules are
    /// only evaluated for such refreshes.
    /// </summary>
    public bool TriggeredByNotification { get; set; }
}