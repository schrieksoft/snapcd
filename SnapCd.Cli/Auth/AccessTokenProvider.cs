// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Cli.Configuration;

namespace SnapCd.Cli.Auth;

/// <summary>
/// A valid access token for a context, from the cache or by refreshing. Never prompts, so a caller
/// such as the Terraform provider cannot hang waiting for a browser.
/// </summary>
public class AccessTokenProvider
{
    // Long enough that a token handed out is still valid when a slow caller first uses it.
    private static readonly TimeSpan RenewBefore = TimeSpan.FromMinutes(5);

    private readonly ITokenStore _store;
    private readonly Func<CliContext, OAuthClient> _clientFor;
    private readonly TimeProvider _time;

    public AccessTokenProvider(ITokenStore store, Func<CliContext, OAuthClient> clientFor, TimeProvider? time = null)
    {
        _store = store;
        _clientFor = clientFor;
        _time = time ?? TimeProvider.System;
    }

    public async Task<StoredTokens> GetAsync(CliContext context, CancellationToken ct)
    {
        var tokens = _store.Get(context.Name);
        if (tokens?.RefreshToken is null)
            throw new CliException($"Not logged in to context '{context.Name}'. Run: snapcd login --context {context.Name}");

        if (tokens.AccessToken is not null && tokens.AccessTokenExpiresOn - RenewBefore > _time.GetUtcNow())
            return tokens;

        TokenSet refreshed;
        try
        {
            refreshed = await _clientFor(context).RefreshAsync(tokens.RefreshToken, ct);
        }
        catch (SessionExpiredException)
        {
            _store.Remove(context.Name);
            throw new CliException($"The session for context '{context.Name}' has expired. Run: snapcd login --context {context.Name}");
        }

        var updated = new StoredTokens
        {
            AccessToken = refreshed.AccessToken,
            AccessTokenExpiresOn = refreshed.ExpiresOn,
            // The Server rotates refresh tokens, so the new one replaces the old.
            RefreshToken = refreshed.RefreshToken ?? tokens.RefreshToken
        };
        _store.Set(context.Name, updated);
        return updated;
    }
}
