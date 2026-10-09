// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Views.Metadata;

/// <summary>Identifies a ModuleJobMissionRun and the rows it joins, for callers who may discover it without reading it.</summary>
public class ModuleJobMissionRunMetadata : EntityMetadataBase
{
    public Guid? AgentConnectionId { get; set; }

    public Guid AgentId { get; set; }

    public Guid InvocationId { get; set; }

    public Guid ModuleJobId { get; set; }

    public Guid ModuleJobMissionId { get; set; }

    public Guid? ServerInstanceId { get; set; }
}
