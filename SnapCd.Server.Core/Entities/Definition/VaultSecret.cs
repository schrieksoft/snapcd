// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using SnapCd.Server.Core.Entities.Definition.Base;

namespace SnapCd.Server.Core.Entities.Definition;

/// <summary>
/// Self-hosted SQL-backed secret store. One row per named secret; ciphertext encrypted at rest
/// via AES-256-GCM using the symmetric key from <c>SecretStore:SqlServer:SymmetricKey</c>.
/// Name is the full scoped identifier produced by <c>SecretService.MakeRemoteSecretName</c>.
/// </summary>
public class VaultSecret : AuditBase
{
    [Required, MaxLength(512)] public required string Name { get; set; }

    /// <summary>AES-GCM ciphertext blob: 12B nonce || ciphertext || 16B tag.</summary>
    [Required] public required byte[] Ciphertext { get; set; }

    /// <summary>Monotonic version identifier assigned on every write. Mirrors Azure's version-per-write semantics.</summary>
    [Required, MaxLength(64)] public required string Version { get; set; }
}
