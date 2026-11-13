// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.Outputs;
using SnapCd.Contracts.Dto.OutputSets;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.Outputs;
using SnapCd.Server.Core.Factories.Vaults;
using SnapCd.Server.Core.Mappers.Outputs;
using SnapCd.Server.Core.Services.Interfaces.Vaults;
using SnapCd.Server.Core.Services.Nonsecured.Vaults;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Outputs;

/// <summary>
/// A sensitive output whose value is empty. The vault refuses to store one, so the row records
/// that the value is blank and no vault call is made in either direction.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class BlankSensitiveOutputTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private readonly Guid _moduleId = Guid.NewGuid();

    public BlankSensitiveOutputTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    private Guid OrganizationId => _fixture.Organizations["0"].Id;

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void A_Blank_Sensitive_Output_Is_Marked_Blank(string? value)
    {
        var (outputSet, _) = Mapper().MapOutputSet(OutputSetWith(value!, sensitive: true), _moduleId, OrganizationId);

        var secret = Assert.IsType<SecretOutput>(Assert.Single(outputSet.Outputs));
        Assert.True(secret.BlankOutput);
        Assert.False(string.IsNullOrEmpty(secret.RemoteSecretName));
    }

    [Fact]
    public void A_Sensitive_Output_With_A_Value_Is_Not_Marked_Blank()
    {
        var (outputSet, _) = Mapper().MapOutputSet(OutputSetWith("hunter2", sensitive: true), _moduleId, OrganizationId);

        Assert.False(Assert.IsType<SecretOutput>(Assert.Single(outputSet.Outputs)).BlankOutput);
    }

    /// <summary>
    /// The reported failure: the value is never handed to the vault, which refuses an empty one.
    /// </summary>
    [Fact]
    public async Task A_Blank_Sensitive_Output_Reads_Back_Empty_Without_A_Vault_Call()
    {
        var vault = new CountingVaultFactory(_fixture);

        var (outputSet, _) = Mapper(vault).MapOutputSet(OutputSetWith("", sensitive: true), _moduleId, OrganizationId);
        var secret = (SecretOutput)outputSet.Outputs.Single();

        var dto = await Mapper(vault).MapOutput(secret, "https://vault.invalid/");

        Assert.Equal(string.Empty, dto.Value);
        Assert.True(dto.Sensitive);
        Assert.Equal(0, vault.Created);
    }

    /// <summary>
    /// A blank value handed to the vault is what the report saw fail, so the guard that makes the
    /// skip necessary is worth holding onto.
    /// </summary>
    [Fact]
    public async Task The_Vault_Still_Refuses_A_Blank_Value()
    {
        var vault = new SqlVault(new FixtureDbContextFactory(_fixture), new byte[32], NullLogger<SqlVault>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() => vault.SetIfChanged($"test--{Guid.NewGuid():N}", ""));
    }

    private CustomOutputMapper Mapper(IVaultFactory? vaultFactory = null) =>
        new(_fixture.CreateDbContext(),
            vaultFactory ?? new CountingVaultFactory(_fixture),
            Options.Create(new SecretStoreSettings()),
            new TestPrincipalProvider(Guid.NewGuid(), PrincipalDiscriminator.User, OrganizationId));

    private static OutputSetCreateDto OutputSetWith(string value, bool sensitive) =>
        new()
        {
            Outputs =
            [
                new OutputCreateDto { Name = "db_password", Type = "string", Sensitive = sensitive, Value = value }
            ]
        };

    /// <summary>Counts the vaults handed out, so "no vault call" can be asserted rather than assumed.</summary>
    private sealed class CountingVaultFactory(Fixture fixture) : IVaultFactory
    {
        public int Created { get; private set; }

        public IVault Create(string vaultUrl)
        {
            Created++;
            return new SqlVault(new FixtureDbContextFactory(fixture), new byte[32], NullLogger<SqlVault>.Instance);
        }
    }

    private sealed class FixtureDbContextFactory(Fixture fixture) : IDbContextFactory<SnapCdDbContext>
    {
        public SnapCdDbContext CreateDbContext() => fixture.CreateDbContext();
    }
}
