// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Entities.Definition.Secrets;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;

namespace SnapCd.Server.Core.Services.Utils.Vaults;

/// <summary>
/// The name a Secret's value is stored under in the vault.
///
/// A read that builds this differently from the write looks in the wrong place and finds nothing,
/// so every caller goes through here rather than formatting the name itself.
/// </summary>
public static class RemoteSecretName
{
    /// <summary>
    /// Key Vault accepts names of at most 127 characters. The longest form this produces is
    /// "namespace--" plus three GUIDs and their separators, which is 124.
    /// </summary>
    public const int MaxLength = 127;

    /// <summary>
    /// The name this Secret's value is stored under. A Secret the sweeper has not reached yet is
    /// still under the scopeless name, which is what its row records.
    /// </summary>
    public static string For(Secret secret, Guid organizationId) =>
        secret.NameContainsScope
            ? Scoped(secret, organizationId)
            : Scopeless(secret, organizationId);

    /// <summary>
    /// Whether a name already carries its owner's id, for the kinds that store their name rather
    /// than deriving it. A scoped name is what lets everything belonging to one owner be found by
    /// prefix when that owner is deleted.
    ///
    /// Decided by the prefix rather than by counting segments, because a signal's name ends in an
    /// output name that may itself contain the separator.
    /// </summary>
    public static bool NameContainsScope(string secretName)
    {
        if (string.IsNullOrEmpty(secretName)) return false;

        var separator = secretName.IndexOf("--", StringComparison.Ordinal);
        if (separator < 0) return false;

        return secretName[..separator] switch
        {
            // Carried the owner from the outset.
            "output" or "signal" or "integration" => true,

            // Scoped only once the sweeper has moved it, so the shape has to be read.
            "module" or "namespace" or "stack" => HasOwnerSegment(secretName),

            _ => false
        };
    }

    /// <summary>
    /// A scoped Secret name is prefix, organization, owner, secret: four segments where the
    /// scopeless form has three. Safe to count here because none of these segments can contain
    /// the separator, all three being GUIDs.
    /// </summary>
    private static bool HasOwnerSegment(string secretName) =>
        secretName.Split("--", StringSplitOptions.None).Length >= 4;

    /// <summary>Carries the owner, so every value belonging to one owner shares a prefix.</summary>
    public static string Scoped(Secret secret, Guid organizationId) =>
        $"{Prefix(secret)}--{organizationId}--{secret.ParentId()}--{secret.Id}";

    /// <summary>The scheme used before names carried their owner.</summary>
    public static string Scopeless(Secret secret, Guid organizationId) =>
        $"{Prefix(secret)}--{organizationId}--{secret.Id}";

    /// <summary>
    /// Every name belonging to one owner, which is what a delete sweeps on. Scoped to the
    /// organization as well, since one vault can serve several.
    /// </summary>
    public static string OwnerPrefix(SecretScope scope, Guid organizationId, Guid ownerId) =>
        $"{Prefix(scope)}--{organizationId}--{ownerId}--";

    /// <summary>
    /// A Module's sensitive output. The name carries no OutputSet, so every apply rewrites one
    /// value rather than adding another.
    /// </summary>
    public static string ForOutput(Guid organizationId, Guid moduleId, string outputName) =>
        OutputPrefix(organizationId, moduleId) + Hash(outputName);

    /// <summary>
    /// Every sensitive output a Module has produced, across all of its OutputSets. Output names
    /// carry the Module rather than the set, so this is the narrowest prefix available for them.
    /// </summary>
    public static string OutputPrefix(Guid organizationId, Guid moduleId) =>
        $"output--{organizationId}--{moduleId}--";

    /// <summary>
    /// A sensitive probe signal: the Module that produced it, then the signal's own id. Same shape
    /// as a Secret, since both are one value belonging to one owner.
    ///
    /// The signal's row id rather than its name, because a signal is a prediction made against one
    /// wave's inputs: the same signal name answers different waves with different values, and each
    /// answer is its own row.
    /// </summary>
    public static string ForSignal(Guid organizationId, Guid moduleId, Guid signalOutputId) =>
        SignalPrefix(organizationId, moduleId) + signalOutputId;

    /// <summary>Every sensitive signal one Module has produced, across every wave.</summary>
    public static string SignalPrefix(Guid organizationId, Guid moduleId) =>
        $"signal--{organizationId}--{moduleId}--";

    /// <summary>
    /// An output's name reduced to something Key Vault accepts, which takes only letters, digits
    /// and hyphens. Output names come from the Module's own code and are not constrained.
    /// </summary>
    public static string Hash(string name)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(name));
        return Convert.ToHexString(bytes).ToLowerInvariant()[..32];
    }

    /// <summary>An Integration's connection blob, which is keyed by the Integration itself.</summary>
    public static string IntegrationPrefix(Guid organizationId, Guid integrationId) =>
        $"integration--{organizationId}--{integrationId}";

    private static string Prefix(Secret secret) => secret switch
    {
        ModuleSecret => "module",
        NamespaceSecret => "namespace",
        StackSecret => "stack",
        _ => throw new InvalidOperationException($"Unknown secret type: {secret.GetType().Name}")
    };

    private static string Prefix(SecretScope scope) => scope switch
    {
        SecretScope.Module => "module",
        SecretScope.Namespace => "namespace",
        SecretScope.Stack => "stack",
        _ => throw new InvalidOperationException($"Unknown secret scope: {scope}")
    };
}
