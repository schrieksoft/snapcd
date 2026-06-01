// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Runner.Settings;

namespace SnapCd.Runner.Services;

public class ModuleDirectoryService
{
    protected readonly string ModuleRootDir;
    protected readonly string InitDir;
    protected readonly string SnapCdDir;
    protected readonly string Subdirectory;
    protected readonly WorkingDirectorySettings WorkingDirectorySettings;

    public ModuleDirectoryService(
        JobMetadata metadata,
        IOptions<WorkingDirectorySettings> workingDirectorySettings
    )
    {
        WorkingDirectorySettings = workingDirectorySettings.Value;
        Subdirectory = metadata.SourceSubdirectory ?? string.Empty;
        var relativeModuleDir = $"{metadata.StackName}/{metadata.NamespaceName}/{metadata.ModuleName}";
        ModuleRootDir = Path.Combine(WorkingDirectorySettings.WorkingDirectory, relativeModuleDir);

        InitDir = string.IsNullOrEmpty(Subdirectory) ? ModuleRootDir : Path.Combine(ModuleRootDir, Subdirectory);
        SnapCdDir = Path.Combine(InitDir, ".snapcd");
    }


    public virtual string GetWorkingDir()
    {
        return WorkingDirectorySettings.WorkingDirectory;
    }

    public virtual string GetTempDir()
    {
        return WorkingDirectorySettings.TempDirectory;
    }

    public virtual string GetModuleRootDir()
    {
        return ModuleRootDir;
    }

    public virtual string GetInitDir()
    {
        return InitDir;
    }

    public virtual string GetSnapCdDir()
    {
        return SnapCdDir;
    }
}