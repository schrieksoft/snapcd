// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;

namespace SnapCd.Server.Core.Views;

public class TerraformModule
{
    [MaxLength(2000)] public string DownloadToDir { get; set; } = string.Empty;

    [MaxLength(2000)] public string RepoSource { get; set; } = string.Empty;

    [MaxLength(255)] public string RepoRevision { get; set; } = string.Empty;

    public TerraformModuleSource SourceType { get; set; }

    public TerraformModuleInfo TerraformModuleInfo { get; set; } = null!;
}