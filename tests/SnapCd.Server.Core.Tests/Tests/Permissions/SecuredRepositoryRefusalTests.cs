// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Permissions;

/// <summary>
/// Secured repositories must refuse with PrincipalNotAuthorizedException, which the controllers
/// translate to 403. UnauthorizedAccessException is caught nowhere, so it surfaces as a 500 and
/// makes an ordinary permission denial look like a server fault.
/// </summary>
public class SecuredRepositoryRefusalTests
{
    [Fact]
    public void No_Secured_Repository_Throws_UnauthorizedAccessException()
    {
        var repositoriesDirectory = RepositoriesDirectory();

        var offenders = Directory
            .EnumerateFiles(repositoriesDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("throw new UnauthorizedAccessException"))
            .Select(path => Path.GetFileName(path))
            .OrderBy(name => name)
            .ToList();

        Assert.Empty(offenders);
    }

    private static string RepositoriesDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "SnapCd.Server.Core")))
            directory = directory.Parent;

        Assert.NotNull(directory);
        return Path.Combine(directory!.FullName, "SnapCd.Server.Core", "Repositories");
    }
}
