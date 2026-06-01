// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Views;

public class JobMetadata
{
    public Guid ExecuteNamespaceSagaId { get; set; }
    public Guid NamespaceJobId { get; set; }
    public Guid ModuleJobId { get; set; }
    public Guid NamespaceId { get; set; }
    public Guid ModuleId { get; set; }
    [MaxLength(255)] public string ModuleName { get; set; } = null!;
    [MaxLength(255)] public string NamespaceName { get; set; } = null!;
    public ExecutionStatus Status { get; set; }
}