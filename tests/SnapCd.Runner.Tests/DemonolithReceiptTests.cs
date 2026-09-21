// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using SnapCd.Runner.Services.SplitMigrate;
using Xunit;

namespace SnapCd.Runner.Tests;

/// <summary>
/// The receipts demonolith writes are where the split's statistics come from, so the field names
/// are a contract. Read against the shape demonolith actually emits.
/// </summary>
public class DemonolithReceiptTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "demonolith-receipt-" + Guid.NewGuid().ToString("N"));

    public DemonolithReceiptTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Reads_A_Map_Receipt()
    {
        Write(DemonolithReceipt.MapReceiptFile, """
            version: 1
            created: "2026-09-11T16:55:35Z"
            tool: demonolith dev
            map: demonolith-refactor-map.yaml
            map_checksum: sha256:18464d48cae35674cd11b13bb85a3e9fa183e5004a29edcf55f17c892e8d7c38
            action: map
            complete: true
            module_states:
                app: modules/.demono/app.tfstate
                networking: modules/.demono/networking.tfstate
            backup_path: modules/.demono/monolith.demono-backup.tfstate
            moves:
                - address: module.storefront_dns
                  module: app
                  outcome: moved
            """);

        var receipt = DemonolithReceipt.Read(_dir, DemonolithReceipt.MapReceiptFile);

        Assert.NotNull(receipt);
        Assert.Equal(1, receipt!.Version);
        Assert.Equal("sha256:18464d48cae35674cd11b13bb85a3e9fa183e5004a29edcf55f17c892e8d7c38", receipt.MapChecksum);
        Assert.True(receipt.Complete);
        Assert.Equal(2, receipt.ModuleStates.Count);
        Assert.Equal("modules/.demono/app.tfstate", receipt.ModuleStates["app"]);
    }

    /// <summary>A step that broke before writing a receipt leaves null, not an exception.</summary>
    [Fact]
    public void Returns_Null_When_The_Step_Wrote_Nothing()
    {
        Assert.Null(DemonolithReceipt.Read(_dir, DemonolithReceipt.ProveReceiptFile));
    }

    [Fact]
    public void Ignores_Fields_It_Does_Not_Know()
    {
        Write(DemonolithReceipt.RunReceiptFile, """
            version: 1
            action: run
            complete: false
            something_new: a field a later demonolith added
            """);

        var receipt = DemonolithReceipt.Read(_dir, DemonolithReceipt.RunReceiptFile);

        Assert.NotNull(receipt);
        Assert.False(receipt!.Complete);
    }

    // A failed run writes a receipt listing the modules it got through. Those pushes stand - the
    // next run skips them - so they are what an operator needs before starting another.
    [Fact]
    public void Reads_The_Pushes_Of_A_Partial_Run()
    {
        Write(DemonolithReceipt.RunReceiptFile, """
            version: 1
            action: run
            complete: false
            pushes:
                - module: networking
                  location: https://host/api/o/state/s/sample-networking
                  outcome: pushed
                - module: database
                  location: https://host/api/o/state/s/sample-database
                  outcome: skipped
            """);

        var receipt = DemonolithReceipt.Read(_dir, DemonolithReceipt.RunReceiptFile);

        Assert.NotNull(receipt);
        Assert.False(receipt!.Complete);
        Assert.Equal(2, receipt.Pushes.Count);
        Assert.Equal("networking", receipt.Pushes[0].Module);
        Assert.Equal("pushed", receipt.Pushes[0].Outcome);
        Assert.Equal("https://host/api/o/state/s/sample-networking", receipt.Pushes[0].Location);
        Assert.Equal("skipped", receipt.Pushes[1].Outcome);
    }

    private void Write(string file, string content) => File.WriteAllText(Path.Combine(_dir, file), content);
}
