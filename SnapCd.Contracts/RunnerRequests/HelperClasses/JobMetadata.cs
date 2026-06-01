// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.RunnerRequests.HelperClasses;

/// <summary>
/// Lightweight metadata about a job for logging and identification.
/// Replaces sending the full ResolvedModule object to runners.
/// </summary>
public class JobMetadata
{
    public required string ModuleName { get; set; }
    public required string NamespaceName { get; set; }
    public required string StackName { get; set; }
    public Guid ModuleId { get; set; }
    public required string? SourceSubdirectory { get; set; }
}
