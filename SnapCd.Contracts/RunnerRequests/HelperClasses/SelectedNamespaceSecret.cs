// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.RunnerRequests.HelperClasses;

public class SelectedNamespaceSecret
{
    public required string InputName { get; set; }
    
    public required Guid InputId { get; set; }

    public required NamespaceInputUsageMode UsageMode { get; set; }
    public required SecretDiscriminator Discriminator { get; set; }

    public required InputType Type { get; set; }

    public required string NamespaceName { get; set; }

    public required Guid NamespaceId { get; set; }

    public Guid? SecretId { get; set; }

    public string? SecretName { get; set; }

    public required string Hash { get; set; }
}