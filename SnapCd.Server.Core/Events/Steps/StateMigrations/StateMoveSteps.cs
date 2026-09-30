// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Steps.Base;

namespace SnapCd.Server.Core.Events.Steps.StateMigrations;

/// <summary>One address to act on, and where to. A remove names only the address.</summary>
public class AddressInstruction
{
    public string Address { get; set; } = null!;

    public string? Target { get; set; }
}

/// <summary>
/// A batch of addresses to act on. Each address runs on its own, so a reply says which of them
/// worked rather than whether the batch did.
/// </summary>
public abstract class TerraformStateMigrationRequestBase : StepRequestBase
{
    public List<AddressInstruction> Instructions { get; set; } = [];
}

/// <summary>What the batch managed, address by address.</summary>
public abstract class TerraformStateMigrationResponseBase : StepResponseBase
{
    public List<AddressResult> Results { get; set; } = [];
}

public class MoveRequested : TerraformStateMigrationRequestBase;

public class MoveCompleted : TerraformStateMigrationResponseBase;

public class MoveCancelled : StepResponseBase;

public class MoveFaulted : StepFaultedBase;

public class ImportRequested : TerraformStateMigrationRequestBase;

public class ImportCompleted : TerraformStateMigrationResponseBase;

public class ImportCancelled : StepResponseBase;

public class ImportFaulted : StepFaultedBase;

public class RemoveRequested : TerraformStateMigrationRequestBase;

public class RemoveCompleted : TerraformStateMigrationResponseBase;

public class RemoveCancelled : StepResponseBase;

public class RemoveFaulted : StepFaultedBase;

/// <summary>
/// What an edit would do, asked before anyone is asked to approve it. A move and a remove ask the
/// engine for a dry run; an import has none, so its check is that the addresses are free.
/// </summary>
public class MoveDryRunRequested : TerraformStateMigrationRequestBase;

public class MoveDryRunCompleted : TerraformStateMigrationResponseBase;

public class MoveDryRunCancelled : StepResponseBase;

public class MoveDryRunFaulted : StepFaultedBase;

public class RemoveDryRunRequested : TerraformStateMigrationRequestBase;

public class RemoveDryRunCompleted : TerraformStateMigrationResponseBase;

public class RemoveDryRunCancelled : StepResponseBase;

public class RemoveDryRunFaulted : StepFaultedBase;

public class ImportPreCheckRequested : TerraformStateMigrationRequestBase;

public class ImportPreCheckCompleted : TerraformStateMigrationResponseBase;

public class ImportPreCheckCancelled : StepResponseBase;

public class ImportPreCheckFaulted : StepFaultedBase;
