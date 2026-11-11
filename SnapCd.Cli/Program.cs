// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.CommandLine;
using SnapCd.Cli;
using SnapCd.Cli.Commands;

var services = CliServices.Default();

var root = new RootCommand("The Snap CD command-line interface.");
root.Subcommands.Add(ContextCommands.Create(services));
root.Subcommands.Add(LoginCommands.Login(services));
root.Subcommands.Add(LoginCommands.Logout(services));
root.Subcommands.Add(AccountCommands.Create(services));
root.Subcommands.Add(OrganizationCommands.Create(services));

return await root.Parse(args).InvokeAsync();
