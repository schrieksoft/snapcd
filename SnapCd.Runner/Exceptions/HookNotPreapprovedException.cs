// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Runner.Exceptions;

/// <summary>
/// Exception thrown when a hook fails pre-approval validation.
/// </summary>
public class HookNotPreapprovedException : Exception
{
    public string HookName { get; }
    public string HookContentPreview { get; }

    public HookNotPreapprovedException(string hookName, string hookContent)
        : base($"Hook '{hookName}' is not pre-approved and cannot be executed. " +
               $"Enable hook pre-approval validation and add this hook to the pre-approved hooks directory.")
    {
        HookName = hookName;
        // Only store first 100 characters for security/logging purposes
        HookContentPreview = hookContent.Length > 100
            ? hookContent.Substring(0, 100) + "..."
            : hookContent;
    }
}
