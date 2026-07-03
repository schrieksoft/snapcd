// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Dtos;

public class DependencyGraphEdgeDto
{
    public string DisplayName { get; set; } = null!;
    public Guid ModuleId { get; set; }
    public Guid NamespaceId { get; set; }

    public string ShortDisplayName
    {
        get
        {
            var parts = DisplayName.Split('/');
            return parts.Length >= 3 ? $"{parts[1]}/{parts[2]}" : DisplayName;
        }
    }
}