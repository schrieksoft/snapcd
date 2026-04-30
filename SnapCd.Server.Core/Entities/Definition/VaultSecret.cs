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
