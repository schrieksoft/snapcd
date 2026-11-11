// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.CommandLine;

namespace SnapCd.Cli.Commands;

public static class OrganizationCommands
{
    public static Command Create(CliServices services)
    {
        var command = new Command("organization", "The organization a context works in.");
        command.Subcommands.Add(List(services));
        command.Subcommands.Add(Set(services));
        return command;
    }

    private static Command List(CliServices services)
    {
        var contextOption = LoginCommands.ContextOption();
        var command = new Command("list", "List your organizations on the context's Server. The current one is marked with *.") { contextOption };
        command.SetAction(services.Run(async (parseResult, ct) =>
        {
            var context = services.Config.Load().Resolve(parseResult.GetValue(contextOption));
            var tokens = await services.AccessTokens().GetAsync(context, ct);
            var user = await services.ClientFor(context).GetUserInfoAsync(tokens.AccessToken!, ct);
            foreach (var organization in user.Organizations)
            {
                var marker = organization.Id == context.OrganizationId ? "*" : " ";
                services.Out.WriteLine($"{marker} {organization.Name}  {organization.Id}");
            }
        }));
        return command;
    }

    private static Command Set(CliServices services)
    {
        var contextOption = LoginCommands.ContextOption();
        var organization = new Argument<string>("organization") { Description = "Organization name or ID." };
        var command = new Command("set", "Switch the organization a context works in.") { organization, contextOption };
        command.SetAction(services.Run(async (parseResult, ct) =>
        {
            var config = services.Config.Load();
            var context = config.Resolve(parseResult.GetValue(contextOption));
            var tokens = await services.AccessTokens().GetAsync(context, ct);
            var user = await services.ClientFor(context).GetUserInfoAsync(tokens.AccessToken!, ct);
            var chosen = OrganizationPicker.Pick(user.Organizations, parseResult.GetValue(organization), services.Out, Console.In);

            context.OrganizationId = chosen.Id;
            context.OrganizationName = chosen.Name;
            services.Config.Save(config);
            services.Out.WriteLine($"Context '{context.Name}' now works in organization '{chosen.Name}'.");
        }));
        return command;
    }
}
