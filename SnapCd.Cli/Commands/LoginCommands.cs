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

namespace SnapCd.Cli.Commands;

public static class LoginCommands
{
    public static Option<string> ContextOption() =>
        new("--context") { Description = "Context to use. Defaults to the current one." };

    public static Command Login(CliServices services)
    {
        var contextOption = ContextOption();
        var organizationOption = new Option<string>("--organization")
        {
            Description = "Organization to work in, by name or ID. Asked for when you belong to more than one."
        };
        var noBrowser = new Option<bool>("--no-browser") { Description = "Print the sign-in URL without opening a browser." };

        var command = new Command("login", "Sign in to a Snap CD Server as yourself, through the browser.")
        {
            contextOption, organizationOption, noBrowser
        };
        command.SetAction(services.Run(async (parseResult, ct) =>
        {
            var config = services.Config.Load();
            var context = config.Resolve(parseResult.GetValue(contextOption));
            var client = services.ClientFor(context);

            var tokens = await new BrowserLogin(client, services.Out).RunAsync(!parseResult.GetValue(noBrowser), ct);
            if (tokens.RefreshToken is null)
                throw new CliException("The Server returned no refresh token, so the session could not be kept.");

            var user = await client.GetUserInfoAsync(tokens.AccessToken, ct);
            var organization = OrganizationPicker.Pick(
                user.Organizations, parseResult.GetValue(organizationOption), services.Out, Console.In);

            services.Tokens.Set(context.Name, new StoredTokens
            {
                AccessToken = tokens.AccessToken,
                AccessTokenExpiresOn = tokens.ExpiresOn,
                RefreshToken = tokens.RefreshToken
            });

            context.UserEmail = user.Email;
            context.OrganizationId = organization.Id;
            context.OrganizationName = organization.Name;
            services.Config.Save(config);

            services.Out.WriteLine($"Logged in to '{context.Name}' as {user.Email ?? "(no email)"}, organization '{organization.Name}'.");
            if (services.Tokens.IsUnprotected)
                services.Out.WriteLine(
                    $"Warning: no secret store is available, so the session is kept in a file only you can read, in {services.Config.Directory}.");
        }));
        return command;
    }

    public static Command Logout(CliServices services)
    {
        var contextOption = ContextOption();
        var command = new Command("logout", "Forget the session of a context.") { contextOption };
        command.SetAction(services.Run((parseResult, _) =>
        {
            var config = services.Config.Load();
            var context = config.Resolve(parseResult.GetValue(contextOption));
            services.Tokens.Remove(context.Name);
            context.UserEmail = null;
            services.Config.Save(config);
            services.Out.WriteLine($"Logged out of '{context.Name}'.");
            return Task.CompletedTask;
        }));
        return command;
    }
}

public static class OrganizationPicker
{
    /// <summary>The requested organization, the only one, or the one the user picks from a list.</summary>
    public static Organization Pick(IReadOnlyList<Organization> organizations, string? requested, TextWriter output, TextReader input)
    {
        if (organizations.Count == 0)
            throw new CliException("You are not a member of any organization on this Server.");

        if (requested is not null)
            return organizations.FirstOrDefault(o =>
                       o.Id.ToString().Equals(requested, StringComparison.OrdinalIgnoreCase) ||
                       o.Name.Equals(requested, StringComparison.Ordinal))
                   ?? throw new CliException(
                       $"You are not a member of an organization '{requested}'. Yours: {string.Join(", ", organizations.Select(o => o.Name))}");

        if (organizations.Count == 1)
            return organizations[0];

        if (Console.IsInputRedirected && ReferenceEquals(input, Console.In))
            throw new CliException("You belong to more than one organization. Choose one with --organization.");

        output.WriteLine("You belong to more than one organization:");
        for (var i = 0; i < organizations.Count; i++)
            output.WriteLine($"  {i + 1}. {organizations[i].Name}");

        while (true)
        {
            output.Write($"Choose 1-{organizations.Count}: ");
            var line = input.ReadLine();
            if (line is null)
                throw new CliException("No organization was chosen.");
            if (int.TryParse(line.Trim(), out var choice) && choice >= 1 && choice <= organizations.Count)
                return organizations[choice - 1];
        }
    }
}
