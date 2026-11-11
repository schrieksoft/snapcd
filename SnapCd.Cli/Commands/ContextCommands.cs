// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.CommandLine;
using SnapCd.Cli.Configuration;

namespace SnapCd.Cli.Commands;

public static class ContextCommands
{
    public static Command Create(CliServices services)
    {
        var command = new Command("context", "Manage the Servers and organizations the CLI works with.");
        command.Subcommands.Add(Add(services));
        command.Subcommands.Add(List(services));
        command.Subcommands.Add(Set(services));
        command.Subcommands.Add(Use(services));
        command.Subcommands.Add(Remove(services));
        return command;
    }

    private static Command Add(CliServices services)
    {
        var name = new Argument<string>("name") { Description = "Name of the context." };
        var url = new Option<string>("--url") { Description = "URL of the Snap CD Server.", Required = true };
        var insecure = new Option<bool>("--insecure-skip-verify")
        {
            Description = "Do not verify the Server's TLS certificate. For test Servers with a self-signed certificate only."
        };

        var command = new Command("add", "Add a context. The organization is chosen at login.") { name, url, insecure };
        command.SetAction(services.Run((parseResult, _) =>
        {
            var contextName = parseResult.GetValue(name)!;
            var serverUrl = parseResult.GetValue(url)!.TrimEnd('/');
            if (!Uri.TryCreate(serverUrl, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http"))
                throw new CliException($"'{serverUrl}' is not an http or https URL.");

            var config = services.Config.Load();
            if (config.Find(contextName) is not null)
                throw new CliException($"A context named '{contextName}' already exists.");

            config.Contexts.Add(new CliContext
            {
                Name = contextName,
                Url = serverUrl,
                InsecureSkipVerify = parseResult.GetValue(insecure)
            });
            config.CurrentContext ??= contextName;
            services.Config.Save(config);

            services.Out.WriteLine($"Added context '{contextName}' for {serverUrl}.");
            if (config.CurrentContext == contextName)
                services.Out.WriteLine($"It is the current context. Sign in with: snapcd login");
            return Task.CompletedTask;
        }));
        return command;
    }

    private static Command List(CliServices services)
    {
        var command = new Command("list", "List the contexts. The current one is marked with *.");
        command.SetAction(services.Run((_, _) =>
        {
            var config = services.Config.Load();
            if (config.Contexts.Count == 0)
            {
                services.Out.WriteLine("No contexts. Add one with: snapcd context add <name> --url <server>");
                return Task.CompletedTask;
            }

            foreach (var context in config.Contexts)
            {
                var marker = context.Name == config.CurrentContext ? "*" : " ";
                var organization = context.OrganizationName ?? "(not logged in)";
                var user = context.UserEmail is null ? "" : $"  {context.UserEmail}";
                services.Out.WriteLine($"{marker} {context.Name}  {context.Url}  {organization}{user}");
            }
            return Task.CompletedTask;
        }));
        return command;
    }

    private static Command Set(CliServices services)
    {
        var name = new Argument<string>("name") { Description = "Name of the context." };
        var url = new Option<string?>("--url") { Description = "New URL of the Snap CD Server. Forgets the session." };
        var insecure = new Option<bool?>("--insecure-skip-verify")
        {
            Description = "true to stop verifying the Server's TLS certificate, false to verify it again.",
            Arity = ArgumentArity.ExactlyOne
        };

        var command = new Command("set", "Change a context.") { name, url, insecure };
        command.SetAction(services.Run((parseResult, _) =>
        {
            var config = services.Config.Load();
            var context = config.Resolve(parseResult.GetValue(name));
            var newUrl = parseResult.GetValue(url);
            var newInsecure = parseResult.GetValue(insecure);
            if (newUrl is null && newInsecure is null)
                throw new CliException("Nothing to change. Give --url or --insecure-skip-verify.");

            if (newUrl is not null)
            {
                newUrl = newUrl.TrimEnd('/');
                if (!Uri.TryCreate(newUrl, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http"))
                    throw new CliException($"'{newUrl}' is not an http or https URL.");
                if (newUrl != context.Url)
                {
                    context.Url = newUrl;
                    context.OrganizationId = null;
                    context.OrganizationName = null;
                    context.UserEmail = null;
                    services.Tokens.Remove(context.Name);
                }
            }

            if (newInsecure is { } skip)
                context.InsecureSkipVerify = skip;

            services.Config.Save(config);
            services.Out.WriteLine($"Updated context '{context.Name}': {context.Url}{(context.InsecureSkipVerify ? ", TLS certificate not verified" : "")}.");
            return Task.CompletedTask;
        }));
        return command;
    }

    private static Command Use(CliServices services)
    {
        var name = new Argument<string>("name") { Description = "Name of the context." };
        var command = new Command("use", "Make a context the current one.") { name };
        command.SetAction(services.Run((parseResult, _) =>
        {
            var config = services.Config.Load();
            var context = config.Resolve(parseResult.GetValue(name));
            config.CurrentContext = context.Name;
            services.Config.Save(config);
            services.Out.WriteLine($"Switched to context '{context.Name}'.");
            return Task.CompletedTask;
        }));
        return command;
    }

    private static Command Remove(CliServices services)
    {
        var name = new Argument<string>("name") { Description = "Name of the context." };
        var command = new Command("remove", "Remove a context and forget its session.") { name };
        command.SetAction(services.Run((parseResult, _) =>
        {
            var config = services.Config.Load();
            var context = config.Resolve(parseResult.GetValue(name));
            config.Contexts.Remove(context);
            if (config.CurrentContext == context.Name)
                config.CurrentContext = config.Contexts.FirstOrDefault()?.Name;
            services.Config.Save(config);
            services.Tokens.Remove(context.Name);
            services.Out.WriteLine($"Removed context '{context.Name}'.");
            return Task.CompletedTask;
        }));
        return command;
    }
}
