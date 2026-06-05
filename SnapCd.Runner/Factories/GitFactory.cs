// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Runner.Services;
using SnapCd.Runner.Services.ModuleSourceRefresher;

namespace SnapCd.Runner.Factories;

public class GitFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public GitFactory(
        ILoggerFactory loggerFactory
    )
    {
        _loggerFactory = loggerFactory;
    }


    public Git Create(RunnerTaskContext context)
    {
        var logger = _loggerFactory.CreateLogger<Git>();

        return new Git(
            logger,
            context,
            new GitModuleSourceResolver(_loggerFactory.CreateLogger<GitModuleSourceResolver>())
        );
    }
}