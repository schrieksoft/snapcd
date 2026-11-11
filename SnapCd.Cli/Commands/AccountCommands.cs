// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.CommandLine;
using System.Text.Json;
using SnapCd.Cli.Configuration;

namespace SnapCd.Cli.Commands;

public static class AccountCommands
{
    public static Command Create(CliServices services)
    {
        var command = new Command("account", "The signed-in user.");
        command.Subcommands.Add(Show(services));
        command.Subcommands.Add(GetAccessToken(services));
        return command;
    }

    private static Command Show(CliServices services)
    {
        var contextOption = LoginCommands.ContextOption();
        var command = new Command("show", "Show the user, Server and organization of a context.") { contextOption };
        command.SetAction(services.Run((parseResult, _) =>
        {
            var context = services.Config.Load().Resolve(parseResult.GetValue(contextOption));
            var loggedIn = services.Tokens.Get(context.Name)?.RefreshToken is not null;
            services.Out.WriteLine($"Context:      {context.Name}");
            services.Out.WriteLine($"Server:       {context.Url}");
            services.Out.WriteLine($"User:         {(loggedIn ? context.UserEmail ?? "(no email)" : "(not logged in)")}");
            services.Out.WriteLine($"Organization: {context.OrganizationName ?? "-"} {(context.OrganizationId is { } id ? $"({id})" : "")}");
            return Task.CompletedTask;
        }));
        return command;
    }

    private static Command GetAccessToken(CliServices services)
    {
        var contextOption = LoginCommands.ContextOption();
        var output = new Option<string>("--output")
        {
            Description = "json (the default) carries the token, its expiry, the Server URL and the organization; text is the token alone.",
            DefaultValueFactory = _ => "json"
        };
        output.AcceptOnlyFromAmong("json", "text");

        var command = new Command("get-access-token",
            "Print a valid access token for a context, refreshing it if needed. Never prompts.") { contextOption, output };
        command.SetAction(services.Run(async (parseResult, ct) =>
        {
            var context = services.Config.Load().Resolve(parseResult.GetValue(contextOption));
            if (context.OrganizationId is null)
                throw new CliException($"Context '{context.Name}' has no organization. Run: snapcd login --context {context.Name}");

            var tokens = await services.AccessTokens().GetAsync(context, ct);

            if (parseResult.GetValue(output) == "text")
            {
                services.Out.WriteLine(tokens.AccessToken);
                return;
            }

            services.Out.WriteLine(JsonSerializer.Serialize(new
            {
                accessToken = tokens.AccessToken,
                expiresOn = tokens.AccessTokenExpiresOn.UtcDateTime.ToString("O"),
                tokenType = "Bearer",
                url = context.Url,
                organizationId = context.OrganizationId,
                insecureSkipVerify = context.InsecureSkipVerify
            }, new JsonSerializerOptions { WriteIndented = true }));
        }));
        return command;
    }
}
