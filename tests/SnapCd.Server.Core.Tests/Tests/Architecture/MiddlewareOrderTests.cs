// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Architecture;

/// <summary>
/// Where a piece of middleware sits in the pipeline, asserted against the host's source. Nothing
/// in the type system records the order, and middleware that reads the signed-in user before
/// authentication has run sees an anonymous one on every request, which looks like a permission
/// problem rather than a wiring problem.
/// </summary>
public class MiddlewareOrderTests
{
    /// <summary>
    /// The background jobs dashboard decides by the signed-in user, so it has to be registered
    /// after the middleware that establishes one. Registered earlier it refuses every request
    /// with a 401 and no sign of why.
    /// </summary>
    [Fact]
    public void The_Jobs_Dashboard_Is_Registered_After_Authentication()
    {
        var source = File.ReadAllText(
            Path.Combine(SolutionRoot, "SnapCd.Server.Host", "Program.cs"));

        var authentication = source.IndexOf("app.UseAuthentication()", StringComparison.Ordinal);
        var dashboard = source.IndexOf("app.UseSnapCdHangfireDashboard()", StringComparison.Ordinal);

        Assert.True(authentication >= 0, "UseAuthentication is not registered");
        Assert.True(dashboard >= 0, "UseSnapCdHangfireDashboard is not registered");
        Assert.True(
            dashboard > authentication,
            "The jobs dashboard is registered before UseAuthentication, so every request to it " +
            "is anonymous and refused.");
    }

    private static string SolutionRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "SnapCd.Server.Core")))
                directory = directory.Parent;

            Assert.NotNull(directory);
            return directory!.FullName;
        }
    }
}
