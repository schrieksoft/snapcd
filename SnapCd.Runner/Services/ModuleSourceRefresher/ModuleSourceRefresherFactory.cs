// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;

namespace SnapCd.Runner.Services.ModuleSourceRefresher;

public interface IModuleSourceRefresherFactory
{
    IModuleSourceRefresher Create(SourceType sourceType);
}

public class ModuleSourceRefresherFactory : IModuleSourceRefresherFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public ModuleSourceRefresherFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public IModuleSourceRefresher Create(SourceType sourceType)
    {
        switch (sourceType)
        {
            case SourceType.Git:
                return new GitModuleSourceResolver(_loggerFactory.CreateLogger<GitModuleSourceResolver>());
            default:
                throw new NotImplementedException($"Source refresher for module of type \"{sourceType.ToString()}\" is not implemented.");
        }
    }
}
