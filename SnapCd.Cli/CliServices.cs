// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.CommandLine;
using SnapCd.Cli.Auth;
using SnapCd.Cli.Configuration;

namespace SnapCd.Cli;

/// <summary>What the commands share. The token store opens on first use, since it may touch the OS secret store.</summary>
public class CliServices
{
    private readonly Lazy<ITokenStore> _tokens;

    public CliServices(CliConfigStore config, Func<ITokenStore> openTokens, TextWriter output, TextWriter error)
    {
        Config = config;
        _tokens = new Lazy<ITokenStore>(openTokens);
        Out = output;
        Error = error;
    }

    public CliConfigStore Config { get; }
    public ITokenStore Tokens => _tokens.Value;
    public TextWriter Out { get; }
    public TextWriter Error { get; }

    public static CliServices Default()
    {
        var config = CliConfigStore.Default();
        return new CliServices(config, () => TokenStore.Open(config.Directory), Console.Out, Console.Error);
    }

    public OAuthClient ClientFor(CliContext context) =>
        new(OAuthClient.CreateHttpClient(context), context.Url);

    public AccessTokenProvider AccessTokens() => new(Tokens, ClientFor);

    /// <summary>Runs a command body, printing a <see cref="CliException"/> as a plain error.</summary>
    public Func<ParseResult, CancellationToken, Task<int>> Run(Func<ParseResult, CancellationToken, Task> body) =>
        async (parseResult, ct) =>
        {
            try
            {
                await body(parseResult, ct);
                return 0;
            }
            catch (CliException e)
            {
                await Error.WriteLineAsync($"Error: {e.Message}");
                return 1;
            }
        };
}
