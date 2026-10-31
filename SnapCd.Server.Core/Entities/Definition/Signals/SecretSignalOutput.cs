// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition.Signals;

/// <summary>
/// A signal whose upstream declared the output sensitive, so its value object is held in the
/// secret store rather than in a column. The probe step reads it back to build the inputs
/// document, which puts the value on the runner's disk either way: the secret store is about the
/// database, not about the runner.
///
/// Implements ISecretScoped rather than ISecretOutput: the latter requires an OutputSet, which a
/// signal does not have. ISecretScoped is what the resolver actually consumes.
/// </summary>
public class SecretSignalOutput : SignalOutput, ISecretScoped
{
    [MaxLength(255)] public string RemoteSecretName { get; set; } = null!;

    /// <summary>
    /// When the value was first found to be absent from the vault. Null while the value is there.
    /// Advisory: nothing reads it to decide whether this works, so a stale value misleads rather
    /// than breaks.
    /// </summary>
    public DateTime? RemoteValueMissingSince { get; set; }

    public virtual SecretDiscriminator GetSecretDiscriminator()
    {
        return SecretDiscriminator.SecretSignalOutput;
    }
}
