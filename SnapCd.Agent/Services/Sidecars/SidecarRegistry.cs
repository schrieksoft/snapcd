// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Options;
using SnapCd.Agent.Configuration;

namespace SnapCd.Agent.Services.Sidecars;

/// <summary>
/// Instantiates one <see cref="HttpAgentSidecar"/> per configured sidecar and resolves them by name.
/// </summary>
public sealed class SidecarRegistry
{
    private readonly IReadOnlyDictionary<string, IAgentSidecar> _sidecars;

    public SidecarRegistry(IOptions<AgentOptions> options, IHttpClientFactory httpClientFactory)
    {
        _sidecars = options.Value.Sidecars.ToDictionary(
            s => s.Name,
            IAgentSidecar (s) => new HttpAgentSidecar(s, httpClientFactory),
            StringComparer.OrdinalIgnoreCase);
    }

    public bool TryGet(string name, out IAgentSidecar? sidecar)
    {
        if (_sidecars.TryGetValue(name, out var found))
        {
            sidecar = found;
            return true;
        }
        sidecar = null;
        return false;
    }

    /// <summary>
    /// Resolves the agent's sole registered sidecar. Used when an incoming mission omits
    /// <c>SidecarName</c> — only well-defined if exactly one sidecar is configured.
    /// Returns <c>false</c> for zero or multiple registered sidecars (the caller must report
    /// a <c>NoDefaultSidecar</c> failure rather than guessing).
    /// </summary>
    public bool TryGetSingle(out IAgentSidecar? sidecar)
    {
        if (_sidecars.Count == 1)
        {
            sidecar = _sidecars.Values.Single();
            return true;
        }
        sidecar = null;
        return false;
    }

    public IReadOnlyCollection<IAgentSidecar> All => _sidecars.Values.ToList();
}
