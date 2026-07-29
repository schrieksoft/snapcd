// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;

namespace SnapCd.Contracts.Dto.NamespaceHooks;

/// <summary>DTO for creating a new NamespaceHook (POST operations).</summary>
public class NamespaceHookCreateDto
{
    /// <summary>The lifecycle task this hook applies to.</summary>
    public HookTask Task { get; set; }

    /// <summary>When the hook runs relative to the task.</summary>
    public HookPhase Phase { get; set; }

    /// <summary>The shell script that runs at the configured task and phase. Used as default for all modules in the namespace unless overridden.</summary>
    [MaxLength(8000)] public string Script { get; set; } = null!;

    /// <summary>ID of the parent Namespace.</summary>
    public Guid NamespaceId { get; set; }
}
