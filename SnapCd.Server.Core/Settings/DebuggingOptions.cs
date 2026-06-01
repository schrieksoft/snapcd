// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Settings;

public class DebuggingOptions
{
    /// <summary>
    /// When true AND a debugger is attached, the LicenseService returns a synthetic valid
    /// EnterpriseEdition license bypassing all DB and SaaS checks. Has no effect when no
    /// debugger is attached. Intended for local development only.
    /// </summary>
    public bool ForceEnterpriseLicenseWhenDebuggerAttached { get; set; }
}
